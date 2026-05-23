using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// E-10「ラックイーター / 運命削り」エフェクト。
    ///
    /// 効果：
    ///   命中時にプレイヤーの運ゲージを drainAmount% 削る。
    ///   ゲージ貯め込みを咎めるカード。敵専用。
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin : 0.60
    ///   ProbabilityMax : 0.80
    ///   DrainMin       : 10   （削るゲージ量 % の最小値）
    ///   DrainMax       : 20   （削るゲージ量 % の最大値）
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/GaugeDrain")]
    public class GaugeDrainEffect : CardEffectBase
    {
        [Header("ゲージ削り量（%）")]
        public float DrainMin = 10f;
        public float DrainMax = 20f;

        // ゲージ量で決まるので値強化は無効
        public override bool CanBoostValue => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // Value にドレイン量を持たせる（表示用）
            slot.Value      = isHotMode
                ? Mathf.RoundToInt(DrainMax)
                : Mathf.RoundToInt(Random.Range(DrainMin, DrainMax));

            slot.ValueRange = (Mathf.RoundToInt(DrainMin), Mathf.RoundToInt(DrainMax));
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            bool isHit = instance.TryExecuteEffect(effectIndex);
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit         = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            float drainAmount = instance.GetEffectiveValue(effectIndex);

            // 敵カードがプレイヤーのゲージを削る
            context.Logic.DrainPlayerLuckGauge(drainAmount);

            context.Result.IsHit = true;
            context.Result.SetExtra(ResultKeys.GaugeDrained, drainAmount);

            CustomLogger.Info(
                $"[運命削り] プレイヤーゲージ -{drainAmount}% → 残={context.Logic.LuckGauge:F0}%",
                LogTagUtil.TagCard);
        }
    }
}
