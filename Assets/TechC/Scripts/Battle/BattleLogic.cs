using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
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
        private CardResolver resolver;
        private List<ITurnEffect> activeEffects = new();

        private int playerHp;
        private int enemyHp;
        private int enemyHpMax;
        private bool isBattleActive;
        private int turnCount;
        private EnemyData currentEnemy;
        private IEnemyCardPlacementStrategy enemyPlacementStrategy;

        private int currentTurnEnemyProbabilityReductionRate = 0;
        private float currentTurnLuckGaugeMultiplier = 1f;

        private bool hasCounter = false;
        private float counterProbability = 0f;
        private int counterDamage = 0;

        // ─── ダメージ軽減バッファ ─────────────────────────────────────────
        private int currentTurnDamageReductionRate = 0;

        // ─── ターン中の破砕カウント ────────────────────────────────────────
        private int currentTurnScrapCount = 0;

        /// <summary>このターンにプレイヤーが砕いたカードの枚数。ScrapCannonEffect が参照する。</summary>
        public int CurrentTurnScrapCount => currentTurnScrapCount;

        /// <summary>破砕カウントを1増やす。BattleController の OnCardBroken から呼ぶ。</summary>
        public void IncrementScrapCount() => currentTurnScrapCount++;

        // ─── ターン中の総ヒット数 ─────────────────────────────────────────
        private int currentTurnHitCount = 0;

        /// <summary>このターンの総ヒット数。ComboStrikeEffect が参照する。</summary>
        public int CurrentTurnHitCount => currentTurnHitCount;

        /// <summary>ヒット数を加算する。CardResolver がカード解決後に呼ぶ。</summary>
        public void AddHitCount(int count) => currentTurnHitCount += count;

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

        public void StartBattle(GameContext context)
        {
            CardInstance.ResetIdCounter();
            playerHp = context.PlayerHp;
            PlayerHpMax = context.PlayerHpMax;

            deck = new List<CardData>();
            hand = new List<CardInstance>();
            discardPile = new List<CardData>();
            playZone = new PlayZoneSlot[PlayZoneSize];
            luckGauge = new LuckGaugeModel();
            luckGauge.Add(MainManager.I?.LuckGaugeValue ?? 0f);

            enemyHp = context.CurrentEnemy.Hp;
            enemyHpMax = context.CurrentEnemy.Hp;

            foreach (var pair in context.Deck)
                for (int i = 0; i < pair.Value; i++)
                    deck.Add(pair.Key);

            currentEnemy = context?.CurrentEnemy;
            enemyPlacementStrategy = currentEnemy?.CardDeck?.CreateStrategy();

            luckGauge.OnHotModeChanged += HandleHotModeChanged;

            resolver = new CardResolver(this);
            isBattleActive = true;
            turnCount = 0;
        }

        public TurnData BeginTurn()
        {
            turnCount++;

            foreach (var effect in activeEffects)
                effect.OnTurnStart(this);

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

        public List<CardResolveResult> ConfirmTurn()
        {
            currentTurnDamageReductionRate = 0;
            currentTurnEnemyProbabilityReductionRate = 0;
            currentTurnLuckGaugeMultiplier = 1f;
            hasCounter = false;
            counterProbability = 0f;
            counterDamage = 0;
            // scrapCount / hitCount はここでリセットしない
            // （砕く・ヒットはConfirmTurn前に発生するためEndTurnでリセットする）

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

        public void EndTurn()
        {
            currentTurnScrapCount = 0; // ターン終了時に破砕カウントをリセット
            currentTurnHitCount = 0; // ターン終了時にヒット数をリセット
            luckGauge.TickDown();
            for (int i = 0; i < playZone.Length; i++)
                playZone[i]?.Clear();
            activeEffects.RemoveAll(e => e.IsExpired);
        }

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

        public void TakePlayerDamage(int damage, CardResolveResult result)
        {
            int actualDamage = ApplyReduction(damage);
            playerHp = Mathf.Max(0, playerHp - actualDamage);
            result.PlayerHpAfter = playerHp;

            CustomLogger.Info($"TakePlayerDamage: raw={damage} reduced={actualDamage} hp={playerHp}", LogTagUtil.TagCard);

            if (playerHp <= 0)
            {
                isBattleActive = false;
                result.IsBattleEnd = true;
                result.IsWon = false;
            }
        }

        public void ApplyStatusToEnemy(StatusType type, int duration, int stackCount) { }
        public void ApplyStatusToPlayer(StatusType type, int duration, int stackCount) { }

        public void SetDamageReduction(int rate)
            => currentTurnDamageReductionRate = Mathf.Clamp(rate, 0, 100);

        public void SetLuckGaugeMultiplier(float multiplier)
            => currentTurnLuckGaugeMultiplier = Mathf.Max(0f, multiplier);

        public void AddLuckGauge(float amount)
            => luckGauge.Add(amount * currentTurnLuckGaugeMultiplier);

        public bool TrySpendLuckGauge(float cost) => luckGauge.TrySpend(cost);

        public void AddTurnEffect(ITurnEffect effect) => activeEffects.Add(effect);

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

                int index = UnityEngine.Random.Range(0, deck.Count);
                var cardData = deck[index];
                deck.RemoveAt(index);

                var instance = new CardInstance(cardData);
                bool isHotMode = luckGauge?.IsHotMode ?? false;
                instance.RollValues(isHotMode);

                if (luckGauge?.IsHotMode ?? false)
                    HotModeHandEffect.ApplyToCard(instance, true);

                hand.Add(instance);
            }

            foreach (var effect in activeEffects)
                effect.OnAfterDraw(this, hand);

            CustomLogger.Info(
                $"ドロー完了: 手札={hand.Count}, デッキ={deck.Count}, 捨て札={discardPile.Count}",
                LogTagUtil.TagCard);
        }

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

        // ─── 公開メソッド ────────────────────────────────────────────────

        public void SetEnemyProbabilityReduction(int rate)
            => currentTurnEnemyProbabilityReductionRate = Mathf.Clamp(rate, 0, 100);

        public int EnemyProbabilityReductionRate => currentTurnEnemyProbabilityReductionRate;

        public void RegisterCounter(float probability, int damage)
        {
            hasCounter = true;
            counterProbability = probability;
            counterDamage = damage;
        }

        // ─── 激アツハンドラ ───────────────────────────────────────────

        private void HandleHotModeChanged(bool enable)
        {
            HotModeHandEffect.ApplyToHand(hand, enable);
            CustomLogger.Info(
                enable ? "[BattleLogic] 激アツ開始！手札を最大化" : "[BattleLogic] 激アツ解除。ボーナスリセット",
                LogTagUtil.TagBattle);
        }

        public bool TryCounter(CardResolveResult result)
        {
            if (!hasCounter) return false;

            bool triggered = UnityEngine.Random.value <= counterProbability;
            if (!triggered) return false;

            TakeEnemyDamage(counterDamage, result);

            CustomLogger.Info(
                $"カウンター発動: {counterDamage}ダメージ → 敵HP={EnemyHp}",
                LogTagUtil.TagCard);

            return true;
        }
    }
}