using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 「追い打ちの刃（コンボ始動）」エフェクト。
    ///
    /// 効果：
    ///   60〜80% の確率で (baseDamage + hitCount × bonusPerHit) ダメージ。
    ///   hitCount はこのターンの総ヒット数（このカード自身の分は含まない）。
    ///
    /// 変動値：
    ///   - 確率（ProbabilityMin / ProbabilityMax）
    ///   - 1ヒットあたりの増加量（BonusPerHit）
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin : 0.60
    ///   ProbabilityMax : 0.80
    ///   DamageMin      : 2
    ///   DamageMax      : 2
    ///   BonusPerHit    : 1  （ヒット数 × 1 を加算）
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ComboStrike")]
    public class ComboStrikeEffect : CardEffectBase
    {
        [Header("基礎ダメージ（範囲）")]
        public int DamageMin = 2;
        public int DamageMax = 2;

        [Header("1ヒットあたりの倍率増加")]
        public int BonusPerHit = 1;

        // ヒット数連動なので値の直接強化は意味なし
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
            int hitCount = context.Logic.CurrentTurnHitCount; // このカード解決前のヒット数
            int finalDamage = baseDamage * (1 + hitCount * BonusPerHit);
            CustomLogger.Info(
                $"[追い打ちの刃] 基礎={baseDamage} + ヒット数{hitCount} × {BonusPerHit} = {finalDamage}ダメージ",
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