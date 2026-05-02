using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/ProbabilityEffect")]
    public class ProbabilityEffect : CardEffectBase, ITurnEffect
    {
        public bool IsExpired => isExpired;
        private bool isExpired = false;

        public void OnAfterDraw(BattleLogic logic, List<CardInstance> hand)
        {
            foreach (var card in hand)
            {
                if (card.GetEffectiveProbability(0) != card.OriginalData.Effects[0].ProbabilityMax)
                {
                    CustomLogger.Info($"確率効果発動: {card.OriginalData.CardName} の確率を最大にする", LogTagUtil.TagCard);
                    card.AddBonusProbability(0, card.OriginalData.Effects[0].ProbabilityMax- card.GetEffectiveProbability(0));
                    Debug.Log(card.GetEffectiveProbability(0) );

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
