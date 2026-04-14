using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード効果の基底クラス。
    /// 確率範囲と基礎確率はすべての効果が共通で持つ。
    /// </summary>
    public abstract class CardEffectBase : ScriptableObject
    {
        [Header("確率（範囲）")]
        [Range(0f, 1f)] public float ProbabilityMin = 1f;
        [Range(0f, 1f)] public float ProbabilityMax = 1f;
 
        /// <summary>手札に来た時点で確定した基礎確率（0〜1）</summary>
        public float BaseProbability { get; private set; }
 
        /// <summary>運ゲージ消費で上乗せされた確率ボーナス</summary>
        public float BonusProbability { get; set; }
 
        /// <summary>実効確率（基礎 + ボーナス、上限 1f）</summary>
        public float EffectiveProbability => Mathf.Min(BaseProbability + BonusProbability, 1f);
    }
}