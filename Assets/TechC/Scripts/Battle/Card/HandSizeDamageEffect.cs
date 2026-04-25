using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札枚数 × 乗数のダメージを与える効果。
    /// 解決タイミングで手札枚数を参照するため EvaluateAtResolve = true。
    ///
    /// EvaluateResolve() で「乗数 × 手札枚数」を slot.Value に格納し、
    /// Execute() では GetEffectiveValue() でそのまま取得してダメージに使う。
    ///
    /// リファクタリング変更点：
    ///   Execute シグネチャに EffectExecutionState を追加。State に書き込む。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/HandSizeDamage")]
    public class HandSizeDamageEffect : CardEffectBase
    {
        [Header("乗数（範囲）")]
        public int MultiplierMin = 2;
        public int MultiplierMax = 4;

        public override bool EvaluateAtResolve => true;

        public override void EvaluateResolve(EffectSlot slot, int handCount, bool isHotMode)
        {
            int multiplier = isHotMode
                ? MultiplierMax
                : Random.Range(MultiplierMin, MultiplierMax + 1);

            // 手札枚数 × 乗数を Value に格納（Execute で GetEffectiveValue として取得）
            slot.Value = multiplier * handCount;
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

            int damage = instance.GetEffectiveValue(effectIndex);

            if (context.IsEnemy)
                context.Logic.TakePlayerDamage(damage, context.Result);
            else
                context.Logic.TakeEnemyDamage(damage, context.Result);

            context.Result.IsHit = true;
            context.Result.DamageDealt += damage;
            state.TotalDamageToEnemy += damage;

            CustomLogger.Info(
                $"手札連動ダメージ: {damage} (手札{context.CurrentHandCount}枚) Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 確率のみロール。ダメージ値は EvaluateResolve() で確定する。
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            slot.Value = 0; // EvaluateResolve で上書きされる
        }
    }
}