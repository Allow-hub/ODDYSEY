using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード効果の基底クラス。
    /// 確率範囲はすべての効果が共通で持つ。
    /// </summary>
    public abstract class CardEffectBase : ScriptableObject
    {
        [Header("確率（範囲）")]
        [Range(0f, 1f)] public float ProbabilityMin = 1f;
        [Range(0f, 1f)] public float ProbabilityMax = 1f;

        /// <summary>
        /// 手札に来たときに呼ばれるロール処理
        /// </summary>
        public abstract void RollValue(EffectSlot slot, bool isHotMode);

        /// <summary>
        /// 解決時に値を確定する（必要なEffectだけオーバーライド）
        /// </summary>
        public virtual void EvaluateResolve(EffectSlot slot, int handCount, bool isHotMode) { }

        /// <summary>
        /// 解決時評価が必要かどうか
        /// </summary>
        public virtual bool EvaluateAtResolve => false;

        public abstract void Execute(EffectContext context, int effectIndex);
    }

    /// <summary>
    /// 実行用のコンテキスト。
    /// Execute() が必要とするすべての情報を集約する。
    /// </summary>
    public class EffectContext
    {
        public BattleLogic Logic;
        public CardInstance Source;
        public bool IsEnemy;
        public int SlotIndex;

        /// <summary>現在の手札枚数。HandSizeDamageEffect など解決時評価の効果が参照する。</summary>
        public int CurrentHandCount;

        public CardResolveResult Result;
    }

    public class EffectSlot
    {
        public float RolledProbability;
        public int Value;

        public float BonusProbability;
        public int BonusValue;

        public float EffectiveProbability => Mathf.Min(RolledProbability + BonusProbability, 1f);
        public int EffectiveValue => Value + BonusValue;
    }
}