using System;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札に配られたカード1枚のインスタンス。
    ///
    /// 変更点：
    ///   - OnSlotValueChanged イベントを追加。
    ///     SetBonusValue / SetBonusProbability / AddBonus系 が呼ばれたとき発火し、
    ///     CardView の Update ポーリングを不要にした。
    ///   - ResetIdCounter() を追加。
    ///     バトル開始時に BattleLogic から呼ぶことで、static な nextId を
    ///     バトルをまたいで無限に増え続ける問題を解消する。
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

        /// <summary>
        /// ボーナス値・確率が変化したときに発火する。
        /// CardView がこれを購読することで Update ポーリングを廃止できる。
        /// </summary>
        public event Action OnSlotValueChanged;

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
        /// バトル開始時に呼ぶ。nextId をリセットしてIDの無限増加を防ぐ。
        /// BattleLogic.StartBattle() の先頭で呼ぶこと。
        /// </summary>
        public static void ResetIdCounter() => nextId = 1;

        public void RollValues(bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].RollValue(slots[i], isHotMode);
            IsRolled = true;
        }

        public void EvaluateResolveValues(int handCount, bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].EvaluateResolve(slots[i], handCount, isHotMode);
        }

        public void ExecuteAll(EffectContext context, EffectExecutionState state)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
                OriginalData.Effects[i].Execute(context, state, i);
        }

        // ─── 値の取得 ────────────────────────────────────────────────────
        public float GetEffectiveProbability(int i) => slots[i].EffectiveProbability;
        public int GetEffectiveValue(int i) => slots[i].EffectiveValue;
        public float GetBaseProbability(int i) => slots[i].RolledProbability;
        public int GetBaseValue(int i) => slots[i].Value;
        public float GetBonusProbability(int i) => slots[i].BonusProbability;
        public int GetBonusValue(int i) => slots[i].BonusValue;
        public (int, int) GetBaseValueRange(int i) => slots[i].ValueRange;

        // ─── 値の更新（変更後にイベント発火）────────────────────────────
        public void SetBonusProbability(int i, float v)
        {
            slots[i].BonusProbability = v;
            OnSlotValueChanged?.Invoke();
        }

        public void SetBonusValue(int i, int v)
        {
            slots[i].BonusValue = v;
            OnSlotValueChanged?.Invoke();
        }

        public void AddBonusProbability(int i, float v)
        {
            slots[i].BonusProbability += v;
            OnSlotValueChanged?.Invoke();
        }

        public void AddBonusValue(int i, int v)
        {
            slots[i].BonusValue += v;
            OnSlotValueChanged?.Invoke();
        }

        public bool TryExecuteEffect(int i) => UnityEngine.Random.value <= GetEffectiveProbability(i);

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
            // Reset後も購読者に通知して表示を同期
            OnSlotValueChanged?.Invoke();
        }
    }
}