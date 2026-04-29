using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 直前の効果が外れたときにプレイヤー自身がダメージを受ける効果。
    /// 捨て身カードの「外れたとき自分に5固定ダメージ」を実現する。
    ///
    /// 使い方：CardData.Effects に DamageEffect の次に本クラスを配置する。
    ///   Effects[0] = DamageEffect  (60〜80%で 10〜25 ダメージ)
    ///   Effects[1] = SelfDamageEffect (外れたとき 5 固定)
    /// ※ ProbabilityMin / ProbabilityMax は 1.0 固定にすること（判定不使用）。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/SelfDamage")]
    public class SelfDamageEffect : CardEffectBase
    {
        [Header("自傷ダメージ（固定値）")]
        public int SelfDamage = 5;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            // 直前の Effect がヒット判定を持っていなかった、またはヒットしていたら自傷しない
            if (!state.PreviousEffectHadHitCheck || state.PreviousEffectHit) return;

            if (context.IsEnemy)
                context.Logic.TakeEnemyDamage(SelfDamage, context.Result);
            else
                context.Logic.TakePlayerDamage(SelfDamage, context.Result);

            // 累積値を State に書く（CardResolver が Result.Extras に転写する）
            state.TotalSelfDamage += SelfDamage;

            CustomLogger.Info(
                $"自傷ダメージ: {SelfDamage} Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 固定ダメージなのでロール不要
        }
    }
}