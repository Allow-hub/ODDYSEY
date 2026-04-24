using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札に配られたカード1枚のインスタンス。
    /// CardData への参照 + EffectSlotでロール済み値を保持。
    /// </summary>
    public class CardInstance
    {
        public CardData OriginalData { get; private set; }

        private static int nextId = 1;

        /// <summary>
        /// このインスタンスのユニークID（手札内での識別用）
        /// </summary>
        public int InstanceId { get; private set; }

        /// <summary>
        /// 各Effectごとの値を保持（旧rolled配列の代替）
        /// </summary>
        private EffectSlot[] slots;

        public bool IsRolled { get; private set; }

        /// <summary>
        /// CardData を参照して CardInstance を生成
        /// </summary>
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
        /// すべての効果値をロール（Effectに委譲）
        /// </summary>
        public void RollValues(bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
            {
                OriginalData.Effects[i].RollValue(slots[i], isHotMode);
            }

            IsRolled = true;
        }

        /// <summary>
        /// 解決時評価（Effectに委譲）
        /// </summary>
        public void EvaluateResolveValues(int handCount, bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
            {
                OriginalData.Effects[i].EvaluateResolve(slots[i], handCount, isHotMode);
            }
        }

        /// <summary>
        /// 実効確率
        /// </summary>
        public float GetEffectiveProbability(int effectIndex)
            => slots[effectIndex].EffectiveProbability;

        /// <summary>
        /// 実効値（ダメージ・軽減率・その他すべて共通）
        /// </summary>
        public int GetEffectiveValue(int effectIndex)
            => slots[effectIndex].EffectiveValue;

        /// <summary>
        /// 基礎確率
        /// </summary>
        public float GetBaseProbability(int effectIndex)
            => slots[effectIndex].RolledProbability;

        /// <summary>
        /// 基礎値
        /// </summary>
        public int GetBaseValue(int effectIndex)
            => slots[effectIndex].Value;

        /// <summary>
        /// ボーナス取得
        /// </summary>
        public float GetBonusProbability(int effectIndex)
            => slots[effectIndex].BonusProbability;

        public int GetBonusValue(int effectIndex)
            => slots[effectIndex].BonusValue;

        /// <summary>
        /// ボーナス設定
        /// </summary>
        public void SetBonusProbability(int effectIndex, float bonus)
            => slots[effectIndex].BonusProbability = bonus;

        public void SetBonusValue(int effectIndex, int bonus)
            => slots[effectIndex].BonusValue = bonus;

        /// <summary>
        /// ボーナス加算
        /// </summary>
        public void AddBonusProbability(int effectIndex, float bonus)
            => slots[effectIndex].BonusProbability += bonus;

        public void AddBonusValue(int effectIndex, int bonus)
            => slots[effectIndex].BonusValue += bonus;

        /// <summary>
        /// 確率判定
        /// </summary>
        public bool TryExecuteEffect(int effectIndex)
            => Random.value <= GetEffectiveProbability(effectIndex);

        /// <summary>
        /// 指定インデックスの効果を取得
        /// </summary>
        public T GetEffect<T>(int effectIndex) where T : CardEffectBase
        {
            if (effectIndex >= 0 && effectIndex < OriginalData.Effects.Count)
            {
                if (OriginalData.Effects[effectIndex] is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// リセット（プール再利用用）
        /// </summary>
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