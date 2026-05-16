using System.Collections.Generic;
using TechC.ODDESEY;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// イベントのロジックを管理する純粋C#クラス。
    ///
    /// 変更点：
    ///   - GainCard のとき EventData.DrawSuccessCards / DrawFailureCards で抽選し
    ///     EventResult.DrawnCards に詰めて返す。
    ///   - GameContext へのカード追加は行わない（EventController に委譲）。
    /// </summary>
    public class EventLogic
    {
        private EventData data;
        private GameContext context;
        private int consumedGauge = 0;

        public int ReservedGauge { get; private set; } = 0;
        public int FinalSuccessRate => Mathf.Clamp(data.BaseSuccessRate + ReservedGauge, 0, 100);
        public int CurrentGauge => Mathf.FloorToInt(context?.LuckGauge ?? 0f);
        private int AvailableGauge => CurrentGauge - ReservedGauge;

        // ─── 初期化 ───────────────────────────────────────────────────────

        public void Setup(EventData eventData, GameContext gameContext)
        {
            data = eventData;
            context = gameContext;
            ReservedGauge = 0;
            consumedGauge = 0;
        }

        // ─── 使用予定ゲージ操作 ──────────────────────────────────────────

        public bool TryAddReserved(int amount = 1)
        {
            if (!CanAddReserved()) return false;
            int maxAddable = Mathf.Min(AvailableGauge, 100 - FinalSuccessRate);
            if (maxAddable <= 0) return false;
            ReservedGauge += Mathf.Min(amount, maxAddable);
            return true;
        }

        public bool TryRemoveReserved(int amount = 1)
        {
            if (!CanRemoveReserved()) return false;
            ReservedGauge = Mathf.Max(0, ReservedGauge - amount);
            return true;
        }

        public bool CanAddReserved() => AvailableGauge >= 1 && FinalSuccessRate < 100;
        public bool CanRemoveReserved() => ReservedGauge > 0;

        // ─── 挑戦 ────────────────────────────────────────────────────────

        public EventResult ChallengeAndApply()
        {
            // 1. 消費前に成功率を確定させる
            int successRate = FinalSuccessRate;

            // 2. ゲージ消費
            consumedGauge = ReservedGauge;
            context.SpendGauge(ReservedGauge);
            ReservedGauge = 0;

            // 3. 成功判定
            int roll = Random.Range(0, 100);
            bool success = roll < successRate;

            var resultType = success ? data.SuccessResultType : data.FailureResultType;
            var resultValue = success ? data.SuccessResultValue : data.FailureResultValue;

            // 4. GameContext に反映（GainCard 以外）
            var drawnCards = new List<CardData>();
            if (resultType == EventResultType.GainCard)
            {
                // イベントごとのカード候補から抽選
                drawnCards = success
                    ? data.DrawSuccessCards(resultValue)
                    : data.DrawFailureCards(resultValue);
                // 実際の追加は EventController に委譲
            }
            else
            {
                ApplyToContext(resultType, resultValue);
            }

            // 5. 失敗時の運ゲージ還元
            int refund = 0;
            if (!success && consumedGauge >= 1)
            {
                refund = data.FailureGaugeRefund;
                context.AddGauge(refund);
            }

            return new EventResult
            {
                IsSuccess = success,
                ResultType = resultType,
                ResultValue = resultValue,
                FlavorText = success ? data.SuccessFlavorText : data.FailureFlavorText,
                RefundedGauge = refund,
                DrawnCards = drawnCards,
            };
        }

        // ─── やめる ───────────────────────────────────────────────────────

        public void Cancel() => ReservedGauge = 0;

        // ─── 内部処理 ────────────────────────────────────────────────────

        private void ApplyToContext(EventResultType type, int value)
        {
            switch (type)
            {
                case EventResultType.HealHp: context.HealHp(value); break;
                case EventResultType.DamageHp: context.DamageHp(value); break;
                case EventResultType.GainGauge: context.AddGauge(value); break;
                case EventResultType.LoseGauge: context.SpendGauge(value); break;
            }
        }
    }
}