using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 反撃バフ効果。
    ///
    /// 仕様：
    ///   このカードの使用者（プレイヤーまたは敵）に反撃バフを付与する。
    ///   次のターン中、バフ保持者が攻撃を受けたとき、攻撃者に ReflectDamage ダメージを与える。
    ///   反撃は1回発動すると解除。発動しなかった場合も次のターン終了時に解除。
    ///
    ///   プレイヤーが使用 → プレイヤーが反撃バフを取得（被弾時に敵へカウンター）。
    ///   敵が使用 → 敵が反撃バフを取得（被弾時にプレイヤーへカウンター）。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ReflectBuff")]
    public class ReflectBuffEffect : CardEffectBase
    {
        [Header("反撃ダメージ（範囲）")]
        public int ReflectDamageMin = 3;
        public int ReflectDamageMax = 8;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            bool isHit = context.Source.TryExecuteEffect(effectIndex);
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            int damage = context.Source.GetEffectiveValue(effectIndex);
            bool isOnPlayer = !context.IsEnemy;

            context.Logic.RegisterReflectBuff(damage, isOnPlayer);
            context.Result.IsHit = true;

            CustomLogger.Info(
                $"反撃バフ付与: {(isOnPlayer ? "プレイヤー" : "敵")}が反撃{damage}ダメージを保有",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = isHotMode
                ? ReflectDamageMax
                : Random.Range(ReflectDamageMin, ReflectDamageMax + 1);

            slot.ValueRange = (ReflectDamageMin, ReflectDamageMax);
        }
    }
}
