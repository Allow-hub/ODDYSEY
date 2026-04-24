using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札枚数 × 乗数 のダメージを与える効果。
    /// 手札枚数は解決タイミングまで変動するため EvaluateAtResolve = true とし、
    /// CardInstance.EvaluateResolveValues() で乗数と手札枚数を掛け合わせてから確定する。
    ///
    /// 実装フロー：
    ///   RollValues()          → 確率のみロール。ダメージは 0 のまま。
    ///   EvaluateResolveValues(handCount) → rolledDamages[i] = handCount × rolled乗数 を確定
    ///   Execute()             → GetEffectiveDamage() でダメージ取得・適用
    ///
    /// ゲージ連携：
    ///   - 確率 → GetEffectiveProbability() で上昇（設計指針§3-2 準拠）
    ///   - ダメージ → AddBonusDamage() で上乗せ可能（handCount × 乗数 に加算）
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/HandSizeDamage")]
    public class HandSizeDamageEffect : CardEffectBase
    {
        [Header("手札1枚あたりのダメージ乗数（範囲）")]
        [Tooltip("55〜65%の確率で 手札枚数 × この乗数 のダメージを与える")]
        public int MultiplierMin = 2;
        public int MultiplierMax = 4;

        /// <summary>解決時まで値を確定しない</summary>
        public override bool EvaluateAtResolve => true;

        public override void Execute(EffectContext context, int effectIndex)
        {
            if (!context.Source.TryExecuteEffect(effectIndex))
            {
                context.Result.IsHit = false;
                return;
            }

            // EvaluateResolveValues() 済みの値を取得
            int damage = context.Source.GetEffectiveValue(effectIndex);

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(damage, context.Result);
            else
                context.Logic.TakeEnemyDamage(damage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += damage;

            CustomLogger.Info(
                $"手札連動ダメージ: {damage} (手札{context.CurrentHandCount}枚) Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }
        
        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);
        }

        public override void EvaluateResolve(EffectSlot slot, int handCount, bool isHotMode)
        {
            int mult = isHotMode
                ? MultiplierMax
                : Random.Range(MultiplierMin, MultiplierMax + 1);

            slot.Value = handCount * mult;
        }
    }
}