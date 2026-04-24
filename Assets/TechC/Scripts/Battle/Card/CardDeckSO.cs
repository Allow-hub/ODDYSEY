using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    [CreateAssetMenu(menuName = "ODDESEY/CardDeck")]
    public class CardDeckSO : ScriptableObject
    {
        public List<CardDeckEntry> Entries = new();

        public Dictionary<CardData, int> ToDictionary()
        {
            var dict = new Dictionary<CardData, int>();

            foreach (var entry in Entries)
            {
                if (entry.Card == null) continue;

                if (dict.ContainsKey(entry.Card))
                    dict[entry.Card] += entry.Count;
                else
                    dict[entry.Card] = entry.Count;
            }

            return dict;
        }
    }
}