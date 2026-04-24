using System.Collections;
using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public enum StatusType
    {
        Shock,      // 感電
        Burn,       // 燃焼
        Poison,     // 毒
    }

    public class AppliedStatusInfo
    {
        public StatusType Type;
        public int Duration;
        public int StackCount;
    }

    /// <summary>
    /// 状態異常付与効果。
    /// 確率は固定（ProbabilityMin = ProbabilityMax = 1f）のものが多い。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/Status")]
    public class StatusEffect : CardEffectBase
    {
        public StatusType StatusType;

        [Tooltip("状態異常の持続ターン数")]
        public int Duration = 2;

        [Tooltip("スタック数（重ね掛け対応の場合）")]
        public int StackCount = 1;

        public override void Execute(EffectContext context, int effectIndex)
        {
            var instance = context.Source;

            if (!instance.TryExecuteEffect(effectIndex))
            {
                context.Result.IsHit = false;
                return;
            }

            // -------------------------
            // ロジック適用
            // -------------------------
            if (context.IsEnemy)
                context.Logic.ApplyStatusToPlayer(StatusType, Duration, StackCount);
            else
                context.Logic.ApplyStatusToEnemy(StatusType, Duration, StackCount);

            context.Result.IsHit = true;
            context.Result.AppliedStatuses.Add(new AppliedStatusInfo
            {
                Type = StatusType,
                Duration = Duration,
                StackCount = StackCount
            });

            CustomLogger.Info(
                $"状態異常付与: {StatusType} (Duration:{Duration}, Stack:{StackCount}) Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }
        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 何もしない
        }
    }
}