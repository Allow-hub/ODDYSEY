using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// このターンに受ける敵ダメージを軽減する効果。
    /// 軽減率（0〜100）は EffectSlot.Value に格納し GetEffectiveValue() で取得する。
    ///
    /// リファクタリング変更点：
    ///   軽減率を state.DamageReductionRate に書く。
    ///   CardResolver.FlushStateToResult() が Result.Extras[ResultKeys.ReductionRate] に転写し、
    ///   BattleLogic.SetDamageReduction() は CardResolver の後に呼ばれる想定。
    ///
    /// ※ ProbabilityMin / Max は通常 1.0 固定（外れない）。
    ///   ゲージ連携で軽減率を上乗せする場合は AddBonusValue() を使う。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Defense")]
    public class DefenseEffect : CardEffectBase
    {
        [Header("軽減率（%）")]
        [Range(0, 100)] public int ReductionMin = 20;
        [Range(0, 100)] public int ReductionMax = 60;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;
            int rate = Mathf.Clamp(instance.GetEffectiveValue(effectIndex), 0, 100);

            // BattleLogic に軽減率を通知
            context.Logic.SetDamageReduction(rate);

            // State に書く（CardResolver が Extras に転写）
            state.DamageReductionRate = rate;

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"防御発動: 軽減率={rate}% Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode ? ProbabilityMax : Random.Range(ProbabilityMin, ProbabilityMax);
            slot.Value = isHotMode ? ReductionMax : Random.Range(ReductionMin, ReductionMax + 1);
        }
    }
}