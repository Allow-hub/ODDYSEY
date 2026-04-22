using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// バトルのロジックを管理する
    /// </summary>
    public class BattleLogic
    {
        public event Action<TurnData> OnTurnStarted;
        public event Action<CardResolveResult> OnCardResolved;
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        private List<CardData> masterDeck;

        private List<CardData> deck;
        private List<CardInstance> hand;
        private List<CardData> discardPile;// CardData のリストで捨て札を管理（CardInstance は手札にしか存在しないため）
        private PlayZoneSlot[] playZone;

        private LuckGaugeModel luckGauge;

        private int playerHp;
        private int enemyHp;
        private int enemyHpMax;
        private bool isBattleActive;
        private int turnCount;
        private EnemyData currentEnemy;
        private IEnemyCardPlacementStrategy enemyPlacementStrategy;

        // -------------------------------------------------------
        // 公開プロパティ
        // -------------------------------------------------------
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

        /// <summary>
        /// バトル開始：MainManager から呼ぶ。GameContext を受け取って初期化し、最初のターンを開始する。
        /// </summary>
        /// <param name="context"></param>
        public void StartBattle(GameContext context)
        {
            masterDeck = context.Deck;
            playerHp = context.PlayerHp;
            PlayerHpMax = context.PlayerHpMax;

            deck = new List<CardData>(masterDeck);
            hand = new List<CardInstance>();
            discardPile = new List<CardData>();
            playZone = new PlayZoneSlot[PlayZoneSize];
            luckGauge = new LuckGaugeModel();
            luckGauge.Add(MainManager.I?.LuckGaugeValue ?? 0f); // MainManager から初期値を受け取る

            enemyHp = 20;//test
            enemyHpMax = 20;
            // 敵データと配置戦略を初期化
            currentEnemy = context?.CurrentEnemy;
            enemyPlacementStrategy = currentEnemy?.CardDeck?.CreateStrategy();

            isBattleActive = true;
            turnCount = 0;
        }

        /// <summary>
        /// ターン開始：手札をドローし、敵カードを配置する。BattleController から呼ぶ。
        /// </summary>
        /// <returns></returns>
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
                IsHotMode   = luckGauge.IsHotMode,
                TurnCount = turnCount,
            };
        }

        /// <summary>
        /// ターン確定：プレイゾーンのカードを左から順に解決する。BattleController から呼ぶ。
        /// </summary>
        /// <returns></returns>
        public List<CardResolveResult> ConfirmTurn()
        {
            var results = new List<CardResolveResult>();

            for (int i = 0; i < playZone.Length; i++)
            {
                var slot = playZone[i];
                if (slot == null || slot.IsEmpty) continue;

                var instance = slot.IsEnemyCard
                    ? slot.EnemyCardInstance
                    : slot.PlayerCardInstance;

                var context = new EffectContext
                {
                    Logic = this,
                    Source = instance,
                    IsEnemy = slot.IsEnemyCard,
                    SlotIndex = i,
                    Result = new CardResolveResult
                    {
                        SlotIndex = i,
                        IsPlayer = !slot.IsEnemyCard,
                        IsHit = false,
                        DamageDealt = 0,
                        CardInstanceId = instance.InstanceId,
                    }
                };

                for (int e = 0; e < instance.OriginalData.Effects.Count; e++)
                {
                    var effect = instance.OriginalData.Effects[e];
                    effect.Execute(context, e);
                }

                results.Add(context.Result);//結果をBattleControllerを介してViewに渡したいため

                //捨て札へ移行
                if (!slot.IsEnemyCard)
                {
                    discardPile.Add(instance.OriginalData);
                    hand.Remove(instance);
                }
                if (!isBattleActive) break;
            }

            return results;
        }

        /// <summary>
        /// ターン終了：手札を捨て、プレイゾーンをクリアする。BattleController から呼ぶ。
        /// </summary>
        public void EndTurn()
        {
            luckGauge.TickDown();

            for (int i = 0; i < playZone.Length; i++)
                playZone[i]?.Clear();
        }

        /// <summary>
        /// カードをマックスまで引く（ターン開始時に呼ぶ）。deck が尽きたら discardPile をシャッフルして補充する。
        /// </summary>
        private void DrawToFull()
        {
            while (hand.Count < HandLimit && (deck.Count > 0 || discardPile.Count > 0))
            {
                if (deck.Count == 0)
                    ShuffleDiscardToDeck();

                if (deck.Count == 0) break;

                //CardData を deck から取り出す
                var cardData = deck[0];
                deck.RemoveAt(0);

                //CardInstance を生成してロール
                var instance = new CardInstance(cardData);
                var isHotMode = luckGauge != null ? luckGauge.IsHotMode : false;
                instance.RollValues(isHotMode);

                //hand に追加
                hand.Add(instance);
            }
            CustomLogger.Info($"ドロー完了: 手札枚数={hand.Count}, デッキ枚数={deck.Count}, 捨て札枚数={discardPile.Count}", LogTagUtil.TagCard);
        }

        /// <summary>
        /// discardPile をシャッフルして deck に補充する。deck と discardPile の両方が空の場合は何もしない。
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
            CustomLogger.Info($"シャッフル完了: デッキ枚数={deck.Count}, 捨て札枚数={discardPile.Count}", LogTagUtil.TagCard);
        }

        private void PlaceEnemyCards()
        {
            // 前ターンの敵カードをクリア
            for (int i = 0; i < playZone.Length; i++)
            {
                if (playZone[i] != null && playZone[i].IsEnemyCard)
                    playZone[i].Clear();
            }

            // デッキ・戦略がなければスキップ
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
                var isHotMode = luckGauge != null ? luckGauge.IsHotMode : false;
                instance.RollValues(isHotMode);
                playZone[slotIndex] ??= new PlayZoneSlot();//スロットが まだ作られてなければ生成
                playZone[slotIndex].EnemyCardInstance = instance;
                playZone[slotIndex].IsEnemyCard = true;

                CustomLogger.Info(
                    $"敵カード配置: {cardData.CardName} → Slot {slotIndex}",
                    LogTagUtil.TagBattle);
            }
        }

        /// <summary>敵にダメージを与える</summary>
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

        /// <summary>プレイヤーにダメージを与える</summary>
        public void TakePlayerDamage(int damage, CardResolveResult result)
        {
            playerHp = Mathf.Max(0, playerHp - damage);
            result.PlayerHpAfter = playerHp;
            if (playerHp <= 0)
            {
                isBattleActive = false;
                result.IsBattleEnd = true;
                result.IsWon = false;
            }
        }

        public void ApplyStatusToEnemy(StatusType type, int duration, int stackCount)
        {

        }

        public void ApplyStatusToPlayer(StatusType type, int duration, int stackCount)
        {

        }

        public void AddLuckGauge(float amount) => luckGauge.Add(amount);
    }
}