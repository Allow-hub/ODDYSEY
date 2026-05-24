using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 激アツ状態（運ゲージ100%）中に手札の確率と効果値を最大化する静的ヘルパー。
    ///
    /// ProbabilityEffect / EffectiveEffect との違い：
    ///   あちらは「カードを使った1ターン限り」。
    ///   こちらは「ゲージが0%になるまでずっと」手札全体に適用される。
    /// </summary>
    public static class HotModeHandEffect
    {
        /// <summary>手札全体に激アツ効果を適用・解除する。</summary>
        public static void ApplyToHand(List<CardInstance> hand, bool enable)
        {
            foreach (var card in hand)
                ApplyToCard(card, enable);
        }

        /// <summary>
        /// カード1枚に激アツ効果を適用・解除する。
        /// DrawToFull でドロー時にも呼ぶ。
        /// </summary>
        public static void ApplyToCard(CardInstance card, bool enable)
        {
            for (int i = 0; i < card.OriginalData.Effects.Count; i++)
            {
                var effect = card.OriginalData.Effects[i];

                if (enable)
                {
                    // 確率をレンジ最大値まで引き上げる（不足分だけボーナス加算）
                    float probDiff = effect.ProbabilityMax - card.GetEffectiveProbability(i);
                    if (probDiff > 0f)
                        card.AddBonusProbability(i, probDiff);

                    // 効果値をレンジ最大値まで引き上げる（不足分だけボーナス加算）
                    int valDiff = card.GetBaseValueRange(i).Item2 - card.GetEffectiveValue(i);
                    if (valDiff > 0)
                        card.AddBonusValue(i, valDiff);

                    CustomLogger.Info(
                        $"[激アツ] {card.OriginalData.CardName} slot{i}: 確率={card.GetEffectiveProbability(i):P0} 値={card.GetEffectiveValue(i)}",
                        LogTagUtil.TagCard);
                }
                else
                {
                    // 激アツ解除：ボーナスをゼロにリセット
                    card.SetBonusProbability(i, 0f);
                    card.SetBonusValue(i, 0);

                    CustomLogger.Info(
                        $"[激アツ解除] {card.OriginalData.CardName} slot{i}: ボーナスリセット",
                        LogTagUtil.TagCard);
                }
            }
        }
    }
}
