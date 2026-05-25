using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 次のターン、プレイヤーの限界突破コスト段階を上昇させる効果。
    /// 確定発動（RolledProbability = 1）で、カード解決時に次ターンのレベルボーナスを登録する。
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/BreakCostIncrease")]
    public class BreakCostIncreaseEffect : CardEffectBase
    {
        [Header("段階数レンジ（カードで変動させたい場合に設定）")]
        public int StageMin = 1;
        public int StageMax = 1;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            // 確定発動
            slot.RolledProbability = 1f;

            int min = StageMin;
            int max = StageMax;

            slot.Value = isHotMode ? max : Random.Range(min, max + 1);
            slot.ValueRange = (min, max);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            int stages = Mathf.Max(0, context.Source.GetEffectiveValue(effectIndex));
            if (stages > 0)
                context.Logic.AddNextTurnLimitBreakCostLevelBonus(stages);

            context.Result.IsHit = true;
        }

    }
}
