using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カード効果の基底クラス。
    /// 確率範囲はすべての効果が共通で持つ。
    ///
    /// リファクタリング変更点：
    ///   Execute(context, effectIndex) に加え EffectExecutionState を受け取るようになった。
    ///   Effect間の通信（IsHit など）は State 経由で行い、Result への直接依存を排除した。
    /// </summary>
    public abstract class CardEffectBase : ScriptableObject
    {
        [Header("確率（範囲）")]
        [Range(0f, 1f)] public float ProbabilityMin = 1f;
        [Range(0f, 1f)] public float ProbabilityMax = 1f;

        /// <summary>手札に来たときに呼ばれるロール処理</summary>
        public abstract void RollValue(EffectSlot slot, bool isHotMode);

        /// <summary>解決時に値を確定する（HandSizeDamageEffect など遅延評価が必要な効果だけオーバーライド）</summary>
        public virtual void EvaluateResolve(EffectSlot slot, int handCount, bool isHotMode) { }

        /// <summary>解決時評価が必要かどうか</summary>
        public virtual bool EvaluateAtResolve => false;

        /// <summary>
        /// 効果を実行する。
        /// </summary>
        /// <param name="context">外部リソース（BattleLogic, CardInstance など）へのアクセス手段</param>
        /// <param name="state">このカードの解決フロー内の可変状態（Effect間通信に使う）</param>
        /// <param name="effectIndex">CardData.Effects 内のインデックス</param>
        public abstract void Execute(EffectContext context, EffectExecutionState state, int effectIndex);
    }

    /// <summary>
    /// 効果実行に必要な「外部リソースへのアクセス手段」を集約する。
    /// 可変状態（Effect間通信）は EffectExecutionState に分離した。
    ///
    /// フィールドの追加基準：
    ///   「BattleLogic や CardInstance など、外部オブジェクトへの参照か？」→ここに置く
    ///   「解決フロー中に変化する値か？」→ EffectExecutionState に置く
    /// </summary>
    public class EffectContext
    {
        /// <summary>ダメージ適用・状態異常適用などのゲームロジック</summary>
        public BattleLogic Logic;

        /// <summary>このカードのインスタンス（ロール済み値の取得元）</summary>
        public CardInstance Source;

        /// <summary>敵カードかどうか</summary>
        public bool IsEnemy;

        /// <summary>配置されたスロットのインデックス（配置ボーナス判定に使う）</summary>
        public int SlotIndex;

        /// <summary>
        /// 現在の手札枚数。
        /// HandSizeDamageEffect が EvaluateResolve で使い、Execute 時には state 経由でなく
        /// ここから取得する。「フロー中に変化しない外部情報」なので Context に置く。
        /// </summary>
        public int CurrentHandCount;

        /// <summary>このカードの解決結果（BattleController/View へ返す用）</summary>
        public CardResolveResult Result;
    }

    public class EffectSlot
    {
        public float RolledProbability;
        public int Value;
        public (int,int) ValueRange; //効果の抽選範囲

        public float BonusProbability;
        public int BonusValue;


        public float EffectiveProbability => Mathf.Min(RolledProbability + BonusProbability, 1f);
        public int EffectiveValue => Value + BonusValue;
    }
}