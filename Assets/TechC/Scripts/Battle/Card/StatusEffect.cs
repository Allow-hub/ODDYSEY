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
    /// 状態異常（Shock / Burn / Poison）を付与する効果。
    ///
    /// リファクタリング変更点：
    ///   Execute シグネチャに EffectExecutionState を追加。
    ///   状態異常固有の追加情報が必要になった場合は state.Extras（あれば）か
    ///   Result.SetExtra() を使う。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Status")]
    public class StatusEffect : CardEffectBase
    {
        [Header("付与する状態異常")]
        public StatusType StatusType;

        [Header("継続ターン数")]
        public int Duration = 2;

        [Header("スタック数")]
        public int StackCount = 1;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            bool isHit = instance.TryExecuteEffect(effectIndex);

            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            if (context.IsEnemy)
                context.Logic.ApplyStatusToPlayer(StatusType, Duration, StackCount);
            else
                context.Logic.ApplyStatusToEnemy(StatusType, Duration, StackCount);

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"状態異常付与: {StatusType} Duration={Duration} Stack={StackCount} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = 0; // 状態異常はダメージ値なし
        }
    }
}