using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 連続攻撃エフェクト。
    ///
    /// 仕様：
    ///   HitCount 回それぞれ独立して確率判定し、命中したぶんだけダメージを与える。
    ///   例）35〜50% で 4〜8 ダメージ × 3回判定
    ///
    /// RollValue でロールするのは「1回あたりのダメージ」と「1回あたりの確率」のみ。
    /// 回数（HitCount）は ScriptableObject で固定設定する。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/MultiHitDamage")]
    public class MultiHitDamageEffect : CardEffectBase
    {
        [Header("ダメージ（範囲）")]
        public int DamageMin = 4;
        public int DamageMax = 8;

        [Header("判定回数")]
        [Tooltip("何回独立して確率判定するか")]
        public int HitCount = 3;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;
            int totalDamage = 0;
            int hitCount = 0;

            for (int i = 0; i < HitCount; i++)
            {
                bool isHit = instance.TryExecuteEffect(effectIndex);
                if (!isHit) continue;

                int damage = instance.GetEffectiveValue(effectIndex);

                if (context.IsEnemy)
                    context.Logic.TakePlayerDamage(damage, context.Result);
                else
                    context.Logic.TakeEnemyDamage(damage, context.Result);

                totalDamage += damage;
                hitCount++;

                CustomLogger.Info(
                    $"連続攻撃 {i + 1}回目ヒット: {damage}ダメージ",
                    LogTagUtil.TagCard);

                // バトルが終了したら残り判定を打ち切る
                if (context.Result.IsBattleEnd) break;
            }

            // State・Result への書き込みはまとめて行う
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = hitCount > 0;
            state.TotalDamageToEnemy += totalDamage;

            context.Result.IsHit = hitCount > 0;
            context.Result.DamageDealt += totalDamage;

            CustomLogger.Info(
                $"連続攻撃 結果: {hitCount}/{HitCount}回ヒット, 合計{totalDamage}ダメージ",
                LogTagUtil.TagCard);
        }

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
    }
}
