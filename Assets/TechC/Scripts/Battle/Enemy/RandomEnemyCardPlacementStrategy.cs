using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 毎ターン、デッキからランダムにカードを選び、ランダムなスロットに配置する戦略。
    /// デッキが空の場合は配置しない（将来的にシャッフル補充も追加可）。
    /// </summary>
    public class RandomEnemyCardPlacementStrategy : IEnemyCardPlacementStrategy
    {
        public Dictionary<int, CardData> SelectCards(
            IReadOnlyList<CardData> deck,
            int slotCount,
            int cardsPerTurn)
        {
            var result = new Dictionary<int, CardData>();
            if (deck == null || deck.Count == 0) return result;

            // 配置可能なスロットインデックスをシャッフル
            var availableSlots = new List<int>(slotCount);
            for (int i = 0; i < slotCount; i++) availableSlots.Add(i);
            Shuffle(availableSlots);

            // 配置枚数はスロット数を超えない
            int placeCount = Mathf.Min(cardsPerTurn, slotCount);

            for (int i = 0; i < placeCount; i++)
            {
                // デッキからもランダムに選ぶ
                int cardIndex = Random.Range(0, deck.Count);
                result[availableSlots[i]] = deck[cardIndex];
            }

            return result;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}