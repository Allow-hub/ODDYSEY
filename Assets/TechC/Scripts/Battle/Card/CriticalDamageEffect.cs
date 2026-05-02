using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 確定ダメージ＋クリティカル確率でダメージ倍率をかける効果。
    ///
    ///   ProbabilityMin / Max → クリティカル確率
    ///   BaseDamageMin / Max  → 確定ダメージ部分
    ///   CriticalMultiplier   → クリティカル時のダメージ倍率
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/CriticalDamage")]
    public class CriticalDamageEffect : CardEffectBase
    {
        [Header("確定ダメージ（範囲）")]
        public int BaseDamageMin = 3;
        public int BaseDamageMax = 6;

        [Header("クリティカル倍率")]
        [Tooltip("クリティカル発生時のダメージ倍率")]
        public int CriticalMultiplier = 3;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            int baseDamage = instance.GetEffectiveValue(effectIndex);
            bool isCrit = instance.TryExecuteEffect(effectIndex);

            int finalDamage = isCrit ? baseDamage * CriticalMultiplier : baseDamage;

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(finalDamage, context.Result);
            else
                context.Logic.TakeEnemyDamage(finalDamage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += finalDamage;

            // State に書く（Effect間通信＋CardResolver が Extras に転写）
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = true; // 確定ダメージなので常にヒット扱い
            state.TotalDamageToEnemy += finalDamage;
            state.IsCritical |= isCrit;

            CustomLogger.Info(
                $"クリティカルダメージ: base={baseDamage} isCrit={isCrit} final={finalDamage} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 確率スロットをクリティカル確率として使う
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = isHotMode
                ? BaseDamageMax
                : Random.Range(BaseDamageMin, BaseDamageMax + 1);
            slot.ValueRange = (BaseDamageMin, BaseDamageMax);
        }
    }
}