using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札に配られたカード1枚のインスタンス。
    /// CardData への参照 + EffectSlot でロール済み値を保持する。
    ///
    /// リファクタリング変更点：
    ///   - ExecuteAll() を追加。CardResolver がこれを呼ぶことで、
    ///     解決ループの責任を CardInstance 内に閉じ込めた。
    ///   - EffectExecutionState の生成・管理は CardResolver が行う。
    ///     CardInstance は「値の保持と効果の委譲」に専念する。
    /// </summary>
    public class CardInstance
    {
        public CardData OriginalData { get; private set; }

        private static int nextId = 1;

        /// <summary>このインスタンスのユニークID（手札内での識別用）</summary>
        public int InstanceId { get; private set; }

        /// <summary>各 Effect ごとの値を保持</summary>
        private EffectSlot[] slots;

        public bool IsRolled { get; private set; }

        public CardInstance(CardData data)
        {
            OriginalData = data;
            InstanceId = nextId++;

            int effectCount = data.Effects.Count;
            slots = new EffectSlot[effectCount];
            for (int i = 0; i < effectCount; i++)
                slots[i] = new EffectSlot();
        }

        /// <summary>
        /// このカードのすべての Effect について RollValue() を呼び出し、ランダム値を決定する。
        /// </summary>
        /// <param name="isHotMode">運ゲージがたまっているか</param>
        public void RollValues(bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].RollValue(slots[i], isHotMode);
            IsRolled = true;
        }

        /// <summary>
        /// このカードのすべての Effect について EvaluateResolve() を呼び出し、解決時の値を決定する。
        /// </summary>
        /// <param name="handCount">手札の数</param>
        /// <param name="isHotMode">運ゲージがたまっているか</param>
        public void EvaluateResolveValues(int handCount, bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].EvaluateResolve(slots[i], handCount, isHotMode);
        }

        /// <summary>
        /// このカードのすべての Effect を順番に実行する。
        /// state は CardResolver が生成して渡す（Effect間通信の媒介）。
        /// </summary>
        /// <param name="context">外部リソースへのアクセス手段</param>
        /// <param name="state">効果実行中の可変状態</param>
        public void ExecuteAll(EffectContext context, EffectExecutionState state)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].Execute(context, state, i);
        }

        // ─── 値の取得・更新 ──────────────────────────────────────────────
        public float GetEffectiveProbability(int i) => slots[i].EffectiveProbability;
        public int GetEffectiveValue(int i) => slots[i].EffectiveValue;
        public float GetBaseProbability(int i) => slots[i].RolledProbability;
        public int GetBaseValue(int i) => slots[i].Value;
        public float GetBonusProbability(int i) => slots[i].BonusProbability;
        public int GetBonusValue(int i) => slots[i].BonusValue;
        public (int, int) GetBaseValueRange(int i) => slots[i].ValueRange;

        public void SetBonusProbability(int i, float v) => slots[i].BonusProbability = v;
        public void SetBonusValue(int i, int v) => slots[i].BonusValue = v;
        public void AddBonusProbability(int i, float v) => slots[i].BonusProbability += v;
        public void AddBonusValue(int i, int v) => slots[i].BonusValue += v;

        public bool TryExecuteEffect(int i) => Random.value <= GetEffectiveProbability(i);

        public T GetEffect<T>(int i) where T : CardEffectBase
        {
            if (i >= 0 && i < OriginalData.Effects.Count && OriginalData.Effects[i] is T typed)
                return typed;
            return null;
        }

        public void Reset()
        {
            IsRolled = false;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].RolledProbability = 0;
                slots[i].Value = 0;
                slots[i].BonusProbability = 0;
                slots[i].BonusValue = 0;
            }
        }
    }
}