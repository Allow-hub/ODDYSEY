using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    [CreateAssetMenu(menuName = "CardEffect/CriticalDamage")]
    public class CriticalDamageEffect : CardEffectBase
    {
        [Header("確定ダメージ（範囲）")]
        public int BaseDamageMin = 2;
        public int BaseDamageMax = 3;

        [Header("クリティカル倍率")]
        public int CriticalMultiplier = 3;

        public override void Execute(EffectContext context, int effectIndex)
        {
            int baseDamage = context.Source.GetEffectiveValue(effectIndex);

            // クリティカル判定（確率はここで使う）
            bool isCritical = context.Source.TryExecuteEffect(effectIndex);

            int finalDamage = isCritical
                ? baseDamage * CriticalMultiplier
                : baseDamage;

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(finalDamage, context.Result);
            else
                context.Logic.TakeEnemyDamage(finalDamage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += finalDamage;
            context.Result.IsCritical = isCritical;

            CustomLogger.Info(
                $"クリティカル判定: baseDmg={baseDamage} crit={isCritical} finalDmg={finalDamage} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        /// <summary>
        /// ロール処理をEffect側に移動
        /// </summary>
        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // クリティカル確率
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // 確定ダメージ部分
            slot.Value = isHotMode
                ? BaseDamageMax
                : Random.Range(BaseDamageMin, BaseDamageMax + 1);
        }
    }
}