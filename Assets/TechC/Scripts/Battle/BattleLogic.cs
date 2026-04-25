using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトルのロジックを管理する。
    ///
    /// リファクタリング変更点：
    ///   ConfirmTurn() の解決ループを CardResolver に委譲した。
    ///   BattleLogic は「ゲーム状態の管理」に専念し、
    ///   「カード解決フローの知識」を持たなくてよくなった。
    ///
    ///   新しい解決ロジック（チェーン割り込みなど）は CardResolver を変更すれば済む。
    /// </summary>
    public class BattleLogic
    {
        public event Action<TurnData> OnTurnStarted;
        public event Action<CardResolveResult> OnCardResolved;
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        private List<CardData> deck;
        private List<CardInstance> hand;
        private List<CardData> discardPile;
        private PlayZoneSlot[] playZone;

        private LuckGaugeModel luckGauge;
        private CardResolver resolver; // ← 解決を委譲するクラス

        private int playerHp;
        private int enemyHp;
        private int enemyHpMax;
        private bool isBattleActive;
        private int turnCount;
        private EnemyData currentEnemy;
        private IEnemyCardPlacementStrategy enemyPlacementStrategy;

        // ─── ダメージ軽減バッファ ─────────────────────────────────────────
        private int currentTurnDamageReductionRate = 0;

        // ─── 公開プロパティ ───────────────────────────────────────────────
        public bool IsBattleActive => isBattleActive;
        public int PlayerHp => playerHp;
        public int PlayerHpMax { get; private set; }
        public int EnemyHp => enemyHp;
        public int EnemyHpMax => enemyHpMax;
        public float LuckGauge => luckGauge.Current;
        public float LuckGaugeMax => luckGauge.Max;
        public bool IsHotMode => luckGauge.IsHotMode;

        private const int HandLimit = 5;
        private const int PlayZoneSize = 4;

        // ─────────────────────────────────────────────────────────────────
        // ライフサイクル
        // ─────────────────────────────────────────────────────────────────

        /// <summary>バトル開始：MainManager から呼ぶ。</summary>
        public void StartBattle(GameContext context)
        {
            playerHp = context.PlayerHp;
            PlayerHpMax = context.PlayerHpMax;

            deck = new List<CardData>();
            hand = new List<CardInstance>();
            discardPile = new List<CardData>();
            playZone = new PlayZoneSlot[PlayZoneSize];
            luckGauge = new LuckGaugeModel();
            luckGauge.Add(MainManager.I?.LuckGaugeValue ?? 0f);

            enemyHp = 20; // test
            enemyHpMax = 20;

            foreach (var pair in context.Deck)
                for (int i = 0; i < pair.Value; i++)
                    deck.Add(pair.Key);

            currentEnemy = context?.CurrentEnemy;
            enemyPlacementStrategy = currentEnemy?.CardDeck?.CreateStrategy();

            // CardResolver を生成（this を渡す）
            resolver = new CardResolver(this);

            isBattleActive = true;
            turnCount = 0;
        }

        /// <summary>ターン開始：手札をドローし、敵カードを配置する。</summary>
        public TurnData BeginTurn()
        {
            turnCount++;
            DrawToFull();
            PlaceEnemyCards();

            return new TurnData
            {
                Hand = hand,
                PlayZone = playZone,
                PlayerHp = playerHp,
                PlayerHpMax = PlayerHpMax,
                EnemyHp = enemyHp,
                EnemyHpMax = enemyHpMax,
                LuckGauge = luckGauge.Current,
                IsHotMode = luckGauge.IsHotMode,
                TurnCount = turnCount,
            };
        }

        /// <summary>
        /// ターン確定：プレイゾーンのカードを左から順に解決する。
        ///
        /// 旧実装はここに解決ループを直書きしていたが、CardResolver に委譲した。
        /// BattleLogic は「軽減率のリセット」など状態管理だけを行う。
        /// </summary>
        public List<CardResolveResult> ConfirmTurn()
        {
            // ターン開始時に軽減率をリセット
            currentTurnDamageReductionRate = 0;

            var results = resolver.ResolveAll(
                playZone,
                hand,
                IsHotMode,
                discardCallback: instance =>
                {
                    discardPile.Add(instance.OriginalData);
                    hand.Remove(instance);
                });

            return results;
        }

        /// <summary>ターン終了：プレイゾーンをクリアし、運ゲージをダウンする。</summary>
        public void EndTurn()
        {
            luckGauge.TickDown();
            for (int i = 0; i < playZone.Length; i++)
                playZone[i]?.Clear();
        }

        /// <summary>敵にダメージを与える。</summary>
        public void TakeEnemyDamage(int damage, CardResolveResult result)
        {
            enemyHp = Mathf.Max(0, enemyHp - damage);
            result.EnemyHpAfter = enemyHp;

            if (enemyHp <= 0)
            {
                isBattleActive = false;
                result.IsBattleEnd = true;
                result.IsWon = true;
            }
        }

        /// <summary>プレイヤーにダメージを与える。軽減率を適用する。</summary>
        public void TakePlayerDamage(int damage, CardResolveResult result)
        {
            // 軽減率を適用（0〜100%）
            int actualDamage = ApplyReduction(damage);

            playerHp = Mathf.Max(0, playerHp - actualDamage);
            result.PlayerHpAfter = playerHp;

            Debug.Log($"TakePlayerDamage: raw={damage} reduced={actualDamage} hp={playerHp}");

            if (playerHp <= 0)
            {
                isBattleActive = false;
                result.IsBattleEnd = true;
                result.IsWon = false;
            }
        }

        public void ApplyStatusToEnemy(StatusType type, int duration, int stackCount) { }
        public void ApplyStatusToPlayer(StatusType type, int duration, int stackCount) { }

        /// <summary>
        /// このターンの受けるダメージ軽減率を設定する。
        /// DefenseEffect から呼ばれる。
        /// </summary>
        public void SetDamageReduction(int rate)
        {
            currentTurnDamageReductionRate = Mathf.Clamp(rate, 0, 100);
        }

        public void AddLuckGauge(float amount) => luckGauge.Add(amount);

        /// <summary>
        /// ダメージ軽減率を適用する。
        /// </summary>
        /// <param name="rawDamage"></param>
        /// <returns></returns>
        private int ApplyReduction(int rawDamage)
        {
            if (currentTurnDamageReductionRate <= 0) return rawDamage;
            float multiplier = 1f - currentTurnDamageReductionRate / 100f;
            return Mathf.Max(0, Mathf.RoundToInt(rawDamage * multiplier));
        }

        private void DrawToFull()
        {
            while (hand.Count < HandLimit && (deck.Count > 0 || discardPile.Count > 0))
            {
                if (deck.Count == 0) ShuffleDiscardToDeck();
                if (deck.Count == 0) break;

                var cardData = deck[0];
                deck.RemoveAt(0);

                var instance = new CardInstance(cardData);
                bool isHotMode = luckGauge?.IsHotMode ?? false;
                instance.RollValues(isHotMode);
                hand.Add(instance);
            }

            CustomLogger.Info(
                $"ドロー完了: 手札={hand.Count}, デッキ={deck.Count}, 捨て札={discardPile.Count}",
                LogTagUtil.TagCard);
        }

        /// <summary>
        ///　手札が尽きたとき、捨て札をシャッフルしてデッキに戻す。
        ///　ドロー前に呼ぶこと（DrawToFull から呼ばれる想定）。
        /// </summary>
        private void ShuffleDiscardToDeck()
        {
            deck.AddRange(discardPile);
            discardPile.Clear();

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            CustomLogger.Info(
                $"シャッフル完了: デッキ={deck.Count}, 捨て札={discardPile.Count}",
                LogTagUtil.TagCard);
        }

        /// <summary>
        /// 敵のカードを配置する
        /// </summary>
        private void PlaceEnemyCards()
        {
            for (int i = 0; i < playZone.Length; i++)
                if (playZone[i] != null && playZone[i].IsEnemyCard)
                    playZone[i].Clear();

            if (enemyPlacementStrategy == null || currentEnemy?.CardDeck == null) return;
            if (currentEnemy.CardDeck.Cards == null || currentEnemy.CardDeck.Cards.Count == 0) return;

            var placements = enemyPlacementStrategy.SelectCards(
                currentEnemy.CardDeck.Cards,
                playZone.Length,
                currentEnemy.CardDeck.CardsPerTurn);

            foreach (var (slotIndex, cardData) in placements)
            {
                if (slotIndex < 0 || slotIndex >= playZone.Length) continue;

                var instance = new CardInstance(cardData);
                bool isHotMode = luckGauge?.IsHotMode ?? false;
                instance.RollValues(isHotMode);

                playZone[slotIndex] ??= new PlayZoneSlot();
                playZone[slotIndex].EnemyCardInstance = instance;
                playZone[slotIndex].IsEnemyCard = true;

                CustomLogger.Info(
                    $"敵カード配置: {cardData.CardName} → Slot {slotIndex}",
                    LogTagUtil.TagBattle);
            }
        }
    }
}