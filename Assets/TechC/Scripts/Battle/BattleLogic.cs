using System;
using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class BattleLogic
    {
        // イベント
        public event Action<TurnData> OnTurnStarted;
        // public event Action<CardResolveResult> OnCardResolved;
        // public event Action<float>             OnLuckGaugeChanged;
        public event Action OnBattleWon;
        public event Action OnBattleLost;

        private List<CardData> masterDeck;

        private List<CardData> deck;
        private List<CardInstance> hand;
        private List<CardData> discardPile;// CardData のリストで捨て札を管理（CardInstance は手札にしか存在しないため）
        private PlayZoneSlot[] playZone;

        // private LuckGaugeModel luckGauge;

        private int playerHp;
        private int enemyHp;
        private int enemyHpMax;
        private bool isBattleActive;
        private bool isWon;
        private int turnCount;

        // -------------------------------------------------------
        // 公開プロパティ
        // -------------------------------------------------------

        public bool IsBattleActive => isBattleActive;
        public bool IsWon => isWon;
        // public bool IsHotMode      => luckGauge?.IsHotMode ?? false;
        public int PlayerHp => playerHp;
        public int PlayerHpMax { get; private set; }
        public int EnemyHp => enemyHp;
        public int EnemyHpMax => enemyHpMax;

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
            // luckGauge   = new LuckGaugeModel();

            enemyHp = 20;//test
            enemyHpMax = 20;

            isBattleActive = true;
            isWon = false;
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
                // LuckGauge   = luckGauge.Current,
                // IsHotMode   = luckGauge.IsHotMode,
                TurnCount = turnCount,
            };
        }

        // -------------------------------------------------------
        // カード配置
        // -------------------------------------------------------

        public void SetCardToUse(int slotIndex, int handIndex)
        {
            if (!IsValidSlot(slotIndex) || !IsValidHand(handIndex)) return;

            playZone[slotIndex] ??= new PlayZoneSlot();
            playZone[slotIndex].PlayerCardInstance = hand[handIndex];  // CardInstance を設定
            playZone[slotIndex].IsEnemyCard = false;
        }

        public void SetCardToBreak(int slotIndex, int handIndex)
        {
            if (!IsValidSlot(slotIndex) || !IsValidHand(handIndex)) return;

            playZone[slotIndex] ??= new PlayZoneSlot();
            playZone[slotIndex].PlayerCardInstance = hand[handIndex];  // CardInstance を設定
            playZone[slotIndex].IsEnemyCard = false;
        }

        // -------------------------------------------------------
        // 運ゲージ（未実装のため全停止）
        // -------------------------------------------------------

        /*
        public void SpendLuckForProbability(int slotIndex, float amount)
        {
            if (!IsValidSlot(slotIndex)) return;
            if (!luckGauge.TrySpend(amount)) return;

            playZone[slotIndex].BonusProbability += amount / 100f;
            OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        }

        public void SpendLuckForDamage(int slotIndex, float amount)
        {
            if (!IsValidSlot(slotIndex)) return;
            if (!luckGauge.TrySpend(amount)) return;

            playZone[slotIndex].BonusDamage += (int)(amount / 5f);
            OnLuckGaugeChanged?.Invoke(luckGauge.Current);
        }
        */

        /// <summary>
        /// ターン終了：手札を捨て、プレイゾーンをクリアする。BattleController から呼ぶ。
        /// </summary>
        public void EndTurn()
        {
            /*
            if (luckGauge.IsHotMode)
                luckGauge.TickDown();
            */

            // CardInstance.OriginalData（CardData）を discardPile に戻す
            foreach (var instance in hand)
                discardPile.Add(instance.OriginalData);

            hand.Clear();

            for (int i = 0; i < playZone.Length; i++)
                playZone[i]?.Clear();
        }

        // -------------------------------------------------------
        // 勝敗チェック
        // -------------------------------------------------------

        private void CheckBattleEnd()
        {
            if (enemyHp <= 0)
            {
                isBattleActive = false;
                isWon = true;
                OnBattleWon?.Invoke();
            }
            else if (playerHp <= 0)
            {
                isBattleActive = false;
                isWon = false;
                OnBattleLost?.Invoke();
            }
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
                instance.RollValues();

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
            // TODO
        }

        // -------------------------------------------------------
        // バリデーション
        // -------------------------------------------------------

        private bool IsValidSlot(int index) => index >= 0 && index < playZone.Length;
        private bool IsValidHand(int index) => index >= 0 && index < hand.Count;
    }
}