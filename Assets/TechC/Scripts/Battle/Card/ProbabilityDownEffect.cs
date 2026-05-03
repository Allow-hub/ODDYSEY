using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 確率ダウン効果。
    ///
    /// 仕様：
    ///   このターン中、敵カード全体の命中確率を ReductionRate% 下げる。
    ///   BattleLogic.SetEnemyProbabilityReduction() を呼び、
    ///   CardResolver が敵カードを配置する前に BonusProbability へ反映する。
    ///
    /// RollValue でロールするのは「確率ダウン量」のみ（Value を % として使う）。
    /// ProbabilityMin/Max はこのエフェクト自体の発動確率に使う（通常は 1.0 固定）。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ProbabilityDown")]
    public class ProbabilityDownEffect : CardEffectBase
    {
        [Header("確率ダウン量（範囲）")]
        [Tooltip("敵カードの命中確率を何%下げるか（整数）")]
        public int ReductionRateMin = 20;
        public int ReductionRateMax = 40;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            // 敵カードがこのエフェクトを使うケースは想定しないが念のためガード
            if (context.IsEnemy) return;

            // 発動確率チェック（通常は 1.0 だが設定次第で外れる）
            bool isHit = context.Source.TryExecuteEffect(effectIndex);

            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            int rate = context.Source.GetEffectiveValue(effectIndex);

            // BattleLogic にターン中の確率ダウンを登録
            context.Logic.SetEnemyProbabilityReduction(rate);

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"確率ダウン発動: 敵カード命中確率 -{rate}%",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 発動確率
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // 確率ダウン量（Value を % として利用）
            slot.Value = isHotMode
                ? ReductionRateMax
                : Random.Range(ReductionRateMin, ReductionRateMax + 1);

            slot.ValueRange = (ReductionRateMin, ReductionRateMax);
        }
    }
}