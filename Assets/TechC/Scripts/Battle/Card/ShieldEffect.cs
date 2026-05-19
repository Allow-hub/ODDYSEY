using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// シールド付与エフェクト。
    ///
    /// 効果：
    ///   確率判定なし（確率 100%）で、自分または敵にシールドを付与する。
    ///   IsEnemy が false のとき → プレイヤーにシールド
    ///   IsEnemy が true のとき  → 敵にシールド
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin : 1.0
    ///   ProbabilityMax : 1.0
    ///   ShieldMin      : 5
    ///   ShieldMax      : 10
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Shield")]
    public class ShieldEffect : CardEffectBase
    {
        [Header("シールド量（範囲）")]
        public int ShieldMin = 5;
        public int ShieldMax = 10;

        // シールドは確率強化・値強化どちらも意味ある
        // CanBoostProbability / CanBoostValue はデフォルト true

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 確率はデフォルト 100%
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = isHotMode
                ? ShieldMax
                : Random.Range(ShieldMin, ShieldMax + 1);

            slot.ValueRange = (ShieldMin, ShieldMax);
        }

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

            int shieldAmount = instance.GetEffectiveValue(effectIndex);

            if (context.IsEnemy)
            {
                context.Logic.AddEnemyShield(shieldAmount);
                context.Result.SetExtra(ResultKeys.EnemyShieldGained, shieldAmount);
                context.Result.SetExtra(ResultKeys.EnemyShieldAfter, context.Logic.EnemyShield);
            }
            else
            {
                context.Logic.AddPlayerShield(shieldAmount);
                context.Result.SetExtra(ResultKeys.PlayerShieldGained, shieldAmount);
                context.Result.SetExtra(ResultKeys.PlayerShieldAfter, context.Logic.PlayerShield);
            }

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"[Shield] {(context.IsEnemy ? "敵" : "プレイヤー")} にシールド +{shieldAmount} 付与",
                LogTagUtil.TagCard);
        }
    }
}