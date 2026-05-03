using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カウンター効果。
    ///
    /// 仕様：
    ///   このターン中、敵の攻撃が命中するたびに
    ///   CounterProbability% の確率で CounterDamage ダメージを与える。
    ///
    ///   実際の反撃処理は CardResolver が敵カード解決後に
    ///   BattleLogic.TryCounter() を呼ぶことで行う。
    ///   このエフェクトは「カウンター状態の登録」だけを担う。
    ///
    /// RollValue でロールするのは「発動確率」と「反撃ダメージ」。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Counter")]
    public class CounterEffect : CardEffectBase
    {
        [Header("反撃ダメージ（範囲）")]
        public int CounterDamageMin = 3;
        public int CounterDamageMax = 8;

        // 発動確率は基底クラスの ProbabilityMin / ProbabilityMax を使う

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            if (context.IsEnemy) return;

            // このエフェクト自体の「登録成功確率」チェック（通常は 1.0 固定）
            bool isHit = context.Source.TryExecuteEffect(effectIndex);

            state.PreviousEffectHadHitCheck = true;
            state.PreviousEffectHit = isHit;

            if (!isHit)
            {
                context.Result.IsHit = false;
                return;
            }

            float counterProbability = context.Source.GetEffectiveProbability(effectIndex);
            int counterDamage = context.Source.GetEffectiveValue(effectIndex);

            // BattleLogic にカウンター状態を登録
            // 実際の反撃は CardResolver が敵カード解決後に TryCounter() を呼んで行う
            context.Logic.RegisterCounter(counterProbability, counterDamage);

            context.Result.IsHit = true;

            CustomLogger.Info(
                $"カウンター登録: 発動確率={counterProbability * 100:F0}%, 反撃={counterDamage}ダメージ",
                LogTagUtil.TagCard);
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 発動確率（反撃が起きる確率）
            slot.RolledProbability = isHotMode
                ? ProbabilityMax
                : Random.Range(ProbabilityMin, ProbabilityMax);

            // 反撃ダメージ
            slot.Value = isHotMode
                ? CounterDamageMax
                : Random.Range(CounterDamageMin, CounterDamageMax + 1);

            slot.ValueRange = (CounterDamageMin, CounterDamageMax);
        }
    }
}