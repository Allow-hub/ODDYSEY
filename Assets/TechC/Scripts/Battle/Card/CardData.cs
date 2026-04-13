using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード1枚のデータ定義。
    /// 効果は CardEffectBase のリストで持ち、種類を問わず複数付与できる。
    /// 手札に来た時点で各効果の RollBaseValues() を呼んで基礎数値を確定する。
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "ODDESEY/CardData")]
    public class CardData : ScriptableObject
    {
        [Header("基本情報")]
        public string CardName;
        public Sprite CardSprite;

        [Header("効果リスト")]
        [SerializeReference] public List<CardEffectBase> Effects = new();

        [Header("運ゲージ還元量")]
        [Tooltip("砕いたとき回復する運ゲージ量（0〜100）")]
        [Range(0f, 100f)] public float LuckConversionRate = 20f;

        /// <summary>指定した型の効果を取得する（なければ null）</summary>
        public T GetEffect<T>() where T : CardEffectBase
        {
            foreach (var effect in Effects)
                if (effect is T typed) return typed;
            return null;
        }

        /// <summary>指定した型の効果をすべて取得する</summary>
        public List<T> GetEffects<T>() where T : CardEffectBase
        {
            var result = new List<T>();
            foreach (var effect in Effects)
            {
                if (effect is T typed) result.Add(typed);
            }
            return result;
        }
    }
}