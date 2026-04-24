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
        /// true のとき RollValues() では値を確定せず、
        /// ConfirmTurn()（解決タイミング）で動的に評価する。
        /// 手札枚数依存など解決時まで値が定まらない効果に使う。
        /// </summary>
        public virtual bool EvaluateAtResolve => false;

        /// <summary>
        /// カードが効力を発揮するタイミングで呼ばれる実行メソッド。
        /// </summary>
        /// <param name="context">バトルのデータ</param>
        /// <param name="effectIndex">CardData.Effects 内のインデックス</param>
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
}