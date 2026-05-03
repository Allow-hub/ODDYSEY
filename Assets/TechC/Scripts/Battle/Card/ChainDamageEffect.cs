using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 連鎖攻撃エフェクト。
    ///
    /// 仕様：
    ///   最初に1回判定し、命中したら同じ確率・ダメージで再び判定する。
    ///   これを MaxChain 回まで繰り返す。
    ///   例）65〜80% で 2〜4 ダメージ、命中するたびに再判定（最大5回）
    ///
    /// MultiHitDamage との違い：
    ///   MultiHit → 全回数を最初から判定する（外れても次の回数は継続）
    ///   ChainDamage → 外れた時点でチェーンが途切れる
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ChainDamage")]
    public class ChainDamageEffect : CardEffectBase
    {
        [Header("ダメージ（範囲）")]
        public int DamageMin = 2;
        public int DamageMax = 4;

        [Header("チェーン設定")]
        [Tooltip("命中時に再判定する最大回数（1回目の判定を含めると MaxChain 回が上限）")]
        public int MaxChain = 5;

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            var instance = context.Source;
            int totalDamage = 0;
            int chainCount = 0;

            for (int i = 0; i < MaxChain; i++)
            {
                bool isHit = instance.TryExecuteEffect(effectIndex);
                if (!isHit)
                {
                    CustomLogger.Info(
                        $"連鎖攻撃 {i + 1}回目ミス: チェーン終了",
                        LogTagUtil.TagCard);
                    break; // ミスでチェーン終了
                }

                int damage = instance.GetEffectiveValue(effectIndex);

                if (context.IsEnemy)
                    context.Logic.TakePlayerDamage(damage, context.Result);
                else
                    context.Logic.TakeEnemyDamage(damage, context.Result);

                totalDamage += damage;
                chainCount++;

                CustomLogger.Info(
                    $"連鎖攻撃 {i + 1}回目ヒット: {damage}ダメージ (チェーン継続)",
                    LogTagUtil.TagCard);

                // バトルが終了したら残りチェーンを打ち切る
                if (context.Result.IsBattleEnd) break;
            }

            // State・Result への書き込みはまとめて行う
            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = chainCount > 0;
            state.TotalDamageToEnemy += totalDamage;

            context.Result.IsHit = chainCount > 0;
            context.Result.DamageDealt += totalDamage;

            CustomLogger.Info(
                $"連鎖攻撃 結果: {chainCount}チェーン, 合計{totalDamage}ダメージ",
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
