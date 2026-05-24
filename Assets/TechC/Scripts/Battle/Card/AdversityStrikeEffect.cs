using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// A-13「逆境の一撃」エフェクト。
    ///
    /// 効果：
    ///   30〜40% の確率で 5 ダメージ。
    ///   ただし発動時に運ゲージが gaugeThreshold（デフォルト30%）以下の場合、
    ///   ダメージを damageMultiplier 倍（デフォルト4倍 = 20ダメージ）にする。
    ///
    /// 変動値：
    ///   - 確率（ProbabilityMin / ProbabilityMax）
    ///   - 条件しきい値（gaugeThreshold）
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin  : 0.30
    ///   ProbabilityMax  : 0.40
    ///   DamageMin       : 5
    ///   DamageMax       : 5
    ///   GaugeThreshold  : 30（ゲージ残量30%以下で倍率発動）
    ///   DamageMultiplier: 4
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/AdversityStrike")]
    public class AdversityStrikeEffect : CardEffectBase
    {
        [Header("ダメージ（範囲）")]
        public int DamageMin = 5;
        public int DamageMax = 5;

        [Header("逆境条件")]
        [Tooltip("運ゲージ残量がこの値（%）以下のとき倍率が発動する")]
        [Range(0f, 100f)]
        public float GaugeThreshold = 30f;

        [Tooltip("逆境時のダメージ倍率")]
        [Range(1f, 10f)]
        public float DamageMultiplier = 4f;

        // ダメージは固定（5→20倍率計算）なので値の強化は意味なし
        public override bool CanBoostValue => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = isHotMode
                ? DamageMax
                : Random.Range(DamageMin, DamageMax + 1);

            slot.ValueRange = (DamageMin, DamageMax);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;

            // ─── 命中判定 ─────────────────────────────────────────────────
            bool isHit = instance.TryExecuteEffect(effectIndex);

            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            // ─── ダメージ計算 ─────────────────────────────────────────────
            int baseDamage = instance.GetEffectiveValue(effectIndex);

            // 現在の運ゲージ残量（%）を取得
            float gaugeRatio = context.Logic.LuckGauge / context.Logic.LuckGaugeMax * 100f;
            bool isAdversity = gaugeRatio <= GaugeThreshold;

            int finalDamage = isAdversity
                ? Mathf.RoundToInt(baseDamage * DamageMultiplier)
                : baseDamage;

            CustomLogger.Info(
                $"[逆境の一撃] ゲージ残量={gaugeRatio:F0}% / しきい値={GaugeThreshold}% " +
                $"→ 逆境={isAdversity} / ダメージ={finalDamage}",
                LogTagUtil.TagCard);

            // ─── ダメージ適用 ─────────────────────────────────────────────
            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(finalDamage, context.Result);
            else
                context.Logic.TakeEnemyDamage(finalDamage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += finalDamage;
            state.TotalDamageToEnemy += finalDamage;
        }
    }
}