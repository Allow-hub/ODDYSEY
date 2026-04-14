using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// デッキの先頭から順にカードを取り出し、スロット0番から順に配置する戦略。
    /// デッキを使い切ったら先頭に戻る（ループ）。
    /// 「決まったパターンを繰り返す」ボス戦などに向いている。
    /// </summary>
    public class SequentialEnemyCardPlacementStrategy : IEnemyCardPlacementStrategy
    {
        // 次に引くデッキインデックス（インスタンスをターンをまたいで使い回すことで状態を保持）
        private int deckCursor = 0;

        public Dictionary<int, CardData> SelectCards(
            IReadOnlyList<CardData> deck,
            int slotCount,
            int cardsPerTurn)
        {
            var result = new Dictionary<int, CardData>();
            if (deck == null || deck.Count == 0) return result;

            int placeCount = Mathf.Min(cardsPerTurn, slotCount);

            for (int i = 0; i < placeCount; i++)
            {
                // デッキを循環させる
                result[i] = deck[deckCursor % deck.Count];
                deckCursor++;
            }

            return result;
        }

        /// <summary>
        /// バトル開始時にカーソルをリセットする。
        /// </summary>
        public void Reset() => deckCursor = 0;
    }
}