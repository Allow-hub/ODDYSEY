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

        private int currentTurnEnemyProbabilityReductionRate = 0;
        private float currentTurnLuckGaugeMultiplier = 1f;

        // ─── 敵行動パターン ───────────────────────────────────────────────
        private EnemyActionPattern actionPattern;
        private int lastTurnEnemyDamageTaken = 0;  // 前ターンに敵が受けた累計ダメージ
        private bool isAnnouncedNextTurn = false; // 予告フラグ

        /// <summary>予告フラグを立てる。予告カードの Execute から呼ぶ。</summary>
        public void SetAnnounced() => isAnnouncedNextTurn = true;

        private bool hasCounter = false;
        private float counterProbability = 0f;
        private int counterDamage = 0;

        // ─── ダメージ軽減バッファ ─────────────────────────────────────────
        private int currentTurnDamageReductionRate = 0;

        // ─── 前ターンダメージ記録 ─────────────────────────────────────────
        private int currentTurnEnemyDamageTaken = 0;

        // ─── シールド ─────────────────────────────────────────────────────
        private ShieldModel playerShield = new();
        private ShieldModel enemyShield = new();

        public int PlayerShield => playerShield.Current;
        public int EnemyShield => enemyShield.Current;
        public void AddPlayerShield(int amount) => playerShield.Add(amount);
        public void AddEnemyShield(int amount) => enemyShield.Add(amount);
        public ShieldModel PlayerShieldModel => playerShield;
        public ShieldModel EnemyShieldModel => enemyShield;

        // ─── ターン中の破砕カウント ────────────────────────────────────────
        private int currentTurnScrapCount = 0;
        private readonly HashSet<int> brokenInstanceIds = new();

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
            actionPattern = currentEnemy?.ActionPattern;
            lastTurnEnemyDamageTaken = 0;
            isAnnouncedNextTurn = false;

            luckGauge.OnHotModeChanged += HandleHotModeChanged;

            playerShield.Reset();
            enemyShield.Reset();
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
                    bool isNoise = instance.OriginalData.Effects.Count > 0
                        && instance.OriginalData.Effects[0] is NoiseEffect;
                    if (!isNoise)
                        discardPile.Add(instance.OriginalData);
                    hand.Remove(instance);
                });

            return results;
        }

        public void EndTurn()
        {
            currentTurnScrapCount = 0;
            currentTurnHitCount = 0;
            // 前ターンのダメージを記録してリセット（次ターンの条件評価に使う）
            lastTurnEnemyDamageTaken = currentTurnEnemyDamageTaken;
            currentTurnEnemyDamageTaken = 0;
            isAnnouncedNextTurn = false; // 予告は消費済みにする
            luckGauge.TickDown();
            for (int i = 0; i < playZone.Length; i++)
                playZone[i]?.Clear();
            activeEffects.RemoveAll(e => e.IsExpired);

            // 手札に残ったカードを捨て札へ（ノイズ系・砕いたカードは除外）
            foreach (var instance in hand)
            {
                bool isNoise = instance.OriginalData.Effects.Count > 0
                    && instance.OriginalData.Effects[0] is NoiseEffect;
                if (!isNoise && !brokenInstanceIds.Contains(instance.InstanceId))
                    discardPile.Add(instance.OriginalData);
            }
            hand.Clear();
            brokenInstanceIds.Clear();
        }

        public void TakeEnemyDamage(int damage, CardResolveResult result, bool isPiercing = false)
        {
            int shieldBefore = enemyShield.Current;
            result.SetExtra(ResultKeys.EnemyShieldBefore, shieldBefore);

            int actualDamage = isPiercing ? damage : enemyShield.AbsorbDamage(damage);
            result.SetExtra(ResultKeys.EnemyShieldAfter, enemyShield.Current);

            enemyHp = Mathf.Max(0, enemyHp - actualDamage);
            result.EnemyHpAfter = enemyHp;
            currentTurnEnemyDamageTaken += actualDamage; // 前ターン記録用に加算

            if (enemyHp <= 0)
            {
                isBattleActive = false;
                result.IsBattleEnd = true;
                result.IsWon = true;
            }
        }

        public void TakePlayerDamage(int damage, CardResolveResult result, bool isPiercing = false)
        {
            int reducedDamage = ApplyReduction(damage);
            int shieldBefore = playerShield.Current;
            result.SetExtra(ResultKeys.PlayerShieldBefore, shieldBefore);

            int actualDamage = isPiercing ? reducedDamage : playerShield.AbsorbDamage(reducedDamage);
            result.SetExtra(ResultKeys.PlayerShieldAfter, playerShield.Current);

            playerHp = Mathf.Max(0, playerHp - actualDamage);
            result.PlayerHpAfter = playerHp;

            CustomLogger.Info($"TakePlayerDamage: raw={damage} shield_before={shieldBefore} actual={actualDamage} hp={playerHp}", LogTagUtil.TagCard);

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
            if (deck.Count == 0 && discardPile.Count > 0)
                ShuffleDiscardToDeck();
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
            // 前ターンの敵カードをクリア
            for (int i = 0; i < playZone.Length; i++)
                if (playZone[i] != null && playZone[i].IsEnemyCard)
                    playZone[i].Clear();

            if (actionPattern == null || actionPattern.turns.Count == 0) return;

            var ctx = new EnemyActionContext
            {
                EnemyHpRatio = enemyHpMax > 0 ? (float)enemyHp / enemyHpMax : 1f,
                PlayerGaugeRatio = luckGauge.Max > 0 ? luckGauge.Current / luckGauge.Max : 0f,
                LastTurnDamageTaken = lastTurnEnemyDamageTaken,
                IsAnnounced = isAnnouncedNextTurn,
            };

            var cards = actionPattern.ResolveCards(turnCount, ctx);
            CustomLogger.Info(
                $"[敵行動] ターン{turnCount} : {actionPattern.GetLabel(turnCount)}",
                LogTagUtil.TagBattle);

            // ActionPattern 解決結果を右2スロット（2・3）に配置
            int slotBase = playZone.Length - 2; // 右2枠
            for (int i = 0; i < Mathf.Min(cards.Count, 2); i++)
            {
                int slot = slotBase + i;
                if (slot < 0 || slot >= playZone.Length) continue;
                var instance = new CardInstance(cards[i]);
                instance.RollValues(luckGauge?.IsHotMode ?? false);
                playZone[slot] ??= new PlayZoneSlot();
                playZone[slot].EnemyCardInstance = instance;
                playZone[slot].IsEnemyCard = true;
                CustomLogger.Info($"敵カード配置: {cards[i].CardName} → Slot {slot}", LogTagUtil.TagBattle);
            }
        }

        // ─── 公開メソッド ────────────────────────────────────────────────

        public void SetEnemyProbabilityReduction(int rate)
            => currentTurnEnemyProbabilityReductionRate = Mathf.Clamp(rate, 0, 100);

        /// <summary>
        /// 敵カードからプレイヤーの運ゲージを強制的に削る。
        /// GaugeDrainEffect から呼ぶ。
        /// </summary>
        public void DrainPlayerLuckGauge(float amount)
            => luckGauge.TrySpend(Mathf.Max(0f, amount));

        /// <summary>
        /// プレイヤーの捨て札にカードを追加する。
        /// NoiseInjectEffect / BlackNoiseInjectEffect から呼ぶ。
        /// 次のシャッフル時にデッキに混入する。
        /// </summary>
        public void AddToDiscard(CardData card)
        {
            discardPile.Add(card);
            CustomLogger.Info(
                $"[デッキ妨害] 捨て札に追加: {card.CardName}",
                LogTagUtil.TagCard);
        }

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