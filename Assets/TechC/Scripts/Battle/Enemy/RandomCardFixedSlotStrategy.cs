using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 固定スロットにランダムなカードを配置する戦略。
    /// スロットは後半2つ（slotCount - 2, slotCount - 1）に固定し、
    /// カードはデッキからランダムに選ぶ。
    /// </summary>
    public class RandomCardFixedSlotStrategy : IEnemyCardPlacementStrategy
    {
        public Dictionary<int, CardData> SelectCards(
            IReadOnlyList<CardData> deck,
            int slotCount,
            int cardsPerTurn)
        {
            var result = new Dictionary<int, CardData>();

            // 後半2スロットを使用
            int[] targetSlots = { 2, 3 };

            // デッキからランダムにcardsPerTurn枚を重複なく選ぶ
            var shuffled = new List<CardData>(deck);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            for (int i = 0; i < targetSlots.Length && i < cardsPerTurn && i < shuffled.Count; i++)
            {
                result[targetSlots[i]] = shuffled[i];
            }

            return result;
        }
    }
}