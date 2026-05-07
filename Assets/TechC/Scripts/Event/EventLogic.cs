using TechC.ODDESEY;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    public class EventLogic
    {
        private EventData data;
        private GameContext context;
        private int consumedGauge = 0;

        public int ReservedGauge { get; private set; } = 0;

        /// <summary>最終成功率（0〜100）</summary>
        public int FinalSuccessRate => Mathf.Clamp(data.BaseSuccessRate + ReservedGauge, 0, 100);

        /// <summary>現在の運ゲージ（整数）</summary>
        public int CurrentGauge => Mathf.FloorToInt(context?.LuckGauge ?? 0f);

        /// <summary>現在ゲージのうち未予約の残量</summary>
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

            // 追加できる上限：
            //   ① 未予約残量を超えない
            //   ② 成功率が 100% を超えない
            int maxByGauge = AvailableGauge;
            int maxBySuccessRate = 100 - FinalSuccessRate; // FinalSuccessRate は現在の値
            int maxAddable = Mathf.Min(maxByGauge, maxBySuccessRate);

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

        /// <summary>
        /// ゲージを追加できるか。
        /// 未予約残量が 1% 以上、かつ成功率が 100% 未満。
        /// </summary>
        public bool CanAddReserved()
            => AvailableGauge >= 1 && FinalSuccessRate < 100;

        public bool CanRemoveReserved() => ReservedGauge > 0;

        // ─── 挑戦 ────────────────────────────────────────────────────────

        public EventResult ChallengeAndApply()
        {
            int successRate = FinalSuccessRate;
            // 1. ゲージ消費（判定前に確定させる）
            consumedGauge = ReservedGauge;
            context.SpendGauge(ReservedGauge);
            ReservedGauge = 0;

            // 2. 成功判定
            //    FinalSuccessRate == 100 のとき Random.Range(0,100) は 0〜99 → 常に true
            //    FinalSuccessRate == 0  のとき Random.Range(0,100) は 0〜99 → 常に false
            int roll = Random.Range(0, 100);
            bool success = roll < successRate;

            var resultType = success ? data.SuccessResultType : data.FailureResultType;
            var resultValue = success ? data.SuccessResultValue : data.FailureResultValue;

            // 3. GameContext に反映
            ApplyToContext(resultType, resultValue);

            // 4. 失敗時の運ゲージ還元（消費ゲージが 1% 以上の場合のみ）
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
                case EventResultType.GainCard:  /* EventController に委譲 */ break;
                case EventResultType.None: break;
            }
        }
    }

    public class EventResult
    {
        public bool IsSuccess;
        public EventResultType ResultType;
        public int ResultValue;
        public string FlavorText;
        public int RefundedGauge;
    }
}