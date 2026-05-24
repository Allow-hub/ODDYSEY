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
    ///   - SuccessActions / FailureActions の複数アクションを順番に処理する。
    ///   - 各アクションの結果を EventActionResult のリストで返す。
    ///   - GainCard 以外のアクションは EventLogic 内で Context に反映する。
    ///   - GainCard は DrawnCards に詰めて EventController に委譲（変更なし）。
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
            // 1. 消費前に成功率を確定
            int successRate = FinalSuccessRate;

            // 2. ゲージ消費
            consumedGauge = ReservedGauge;
            context.SpendGauge(ReservedGauge);
            ReservedGauge = 0;

            // 3. 成功判定
            bool success = Random.Range(0, 100) < successRate;

            // 4. 実行するアクションリストを選択
            var actions = success ? data.SuccessActions : data.FailureActions;

            // 5. 各アクションを処理
            var actionResults = new List<EventActionResult>();
            foreach (var action in actions)
            {
                var actionResult = ProcessAction(action);
                actionResults.Add(actionResult);
            }

            // 6. 失敗時の運ゲージ還元
            int refund = 0;
            if (!success && consumedGauge >= 1)
            {
                refund = data.FailureGaugeRefund;
                context.AddGauge(refund);
            }

            return new EventResult
            {
                IsSuccess = success,
                FlavorText = success ? data.SuccessFlavorText : data.FailureFlavorText,
                RefundedGauge = refund,
                ActionResults = actionResults,
            };
        }

        // ─── やめる ───────────────────────────────────────────────────────

        public void Cancel() => ReservedGauge = 0;

        // ─── 内部処理 ────────────────────────────────────────────────────

        /// <summary>
        /// アクション1つを処理する。
        /// GainCard 以外は Context に即時反映し、GainCard は DrawnCards に詰める。
        /// </summary>
        private EventActionResult ProcessAction(EventResultAction action)
        {
            var result = new EventActionResult
            {
                ResultType = action.ResultType,
                ResultValue = action.ResultValue,
            };

            switch (action.ResultType)
            {
                case EventResultType.HealHp:
                    context.HealHp(action.ResultValue);
                    break;

                case EventResultType.DamageHp:
                    context.DamageHp(action.ResultValue);
                    break;

                case EventResultType.GainGauge:
                    context.AddGauge(action.ResultValue);
                    break;

                case EventResultType.LoseGauge:
                    context.SpendGauge(action.ResultValue);
                    break;

                case EventResultType.GainCard:
                    // カード追加は EventController に委譲
                    result.DrawnCards = data.DrawCards(action, action.ResultValue);
                    break;
            }

            return result;
        }
    }
}