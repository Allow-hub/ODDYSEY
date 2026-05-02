using System.Collections;
using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 効果数値を次のターン最大化
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/EffectiveEffect")]
    public class EffectiveEffect : CardEffectBase, ITurnEffect
    {
        public bool IsExpired => isExpired;
        private bool isExpired = false;

        public void OnAfterDraw(BattleLogic logic, List<CardInstance> hand)
        {
            foreach (var card in hand)
            {
                if (card.GetEffectiveProbability(0) != card.OriginalData.Effects[0].ProbabilityMax)
                {
                    CustomLogger.Info($"効果発動: {card.OriginalData.CardName} の効果数値を最大にする", LogTagUtil.TagCard);
                    card.AddBonusValue(0, card.GetBaseValueRange(0).Item2 - card.GetEffectiveValue(0));
                }
            }
            isExpired = true;
        }

        public void OnBeforeDraw(BattleLogic logic)
        {
        }

        public void OnCardResolved(BattleLogic logic, CardResolveResult result)
        {
        }

        public void OnTurnStart(BattleLogic logic)
        {
        }

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = 1;
        }
        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            context.Logic.AddTurnEffect(this);
            CustomLogger.Info($"確率効果が発動: このターンのドローで確率が最大になる", LogTagUtil.TagCard);
        }
    }
}