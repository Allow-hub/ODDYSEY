using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札に配られたカード1枚のインスタンス。
    /// CardData への参照 + ロール済み値を保持。
    /// </summary>
    public class CardInstance
    {
        public CardData OriginalData { get; private set; }
        
        private static int nextId = 1;

        /// <summary>
        /// このインスタンスのユニークID（手札内での識別用）
        /// </summary>
        public int InstanceId { get; private set; }

        // ロール済み値（インデックスは OriginalData.Effects[i] と対応）
        private float[] rolledProbabilities;
        private int[] rolledDamages;

        // 運ゲージによる上乗せ値
        private float[] bonusProbabilities;
        private int[] bonusDamages;

        public bool IsRolled { get; private set; }

        /// <summary>
        /// CardData を参照して CardInstance を生成
        /// </summary>
        public CardInstance(CardData data)
        {
            OriginalData = data;
            InstanceId = nextId++;
            int effectCount = data.Effects.Count;

            rolledProbabilities = new float[effectCount];
            rolledDamages = new int[effectCount];
            bonusProbabilities = new float[effectCount];
            bonusDamages = new int[effectCount];
        }

        /// <summary>
        /// すべての効果値をロール。手札に追加された時点で1度だけ呼ぶ。
        /// EvaluateAtResolve == true の効果は確率のみロールし、値は解決時に確定する。
        /// </summary>
        public void RollValues(bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
            {
                var effect = OriginalData.Effects[i];

                // 確率をロール（EvaluateAtResolve でも確率は手札時に確定）
                rolledProbabilities[i] = isHotMode
                    ? effect.ProbabilityMax
                    : Random.Range(effect.ProbabilityMin, effect.ProbabilityMax);

                // 解決時評価の効果はここでは値を確定しない
                if (effect.EvaluateAtResolve) continue;

                // ダメージをロール（DamageEffect / CriticalDamageEffect）
                if (effect is DamageEffect dmg)
                {
                    rolledDamages[i] = isHotMode
                        ? dmg.DamageMax
                        : Random.Range(dmg.DamageMin, dmg.DamageMax + 1);
                }
                else if (effect is CriticalDamageEffect crit)
                {
                    // 確定ダメージ部分をロール
                    rolledDamages[i] = isHotMode
                        ? crit.BaseDamageMax
                        : Random.Range(crit.BaseDamageMin, crit.BaseDamageMax + 1);
                }
                else if (effect is DefenseEffect def)
                {
                    // 軽減率をロール（rolledDamages を軽減率（%）の格納に流用）
                    rolledDamages[i] = isHotMode
                        ? def.ReductionMax
                        : Random.Range(def.ReductionMin, def.ReductionMax + 1);
                }
            }

            IsRolled = true;
        }

        /// <summary>
        /// 解決時評価の効果の値を確定する。ConfirmTurn() の直前に呼ぶ。
        /// </summary>
        /// <param name="handCount">現在の手札枚数</param>
        /// <param name="isHotMode">激アツモードか</param>
        public void EvaluateResolveValues(int handCount, bool isHotMode = false)
        {
            for (int i = 0; i < OriginalData.Effects.Count; i++)
            {
                var effect = OriginalData.Effects[i];
                if (!effect.EvaluateAtResolve) continue;

                if (effect is HandSizeDamageEffect hs)
                {
                    // 手札枚数 × 乗数をここで確定
                    int multiplier = isHotMode
                        ? hs.MultiplierMax
                        : Random.Range(hs.MultiplierMin, hs.MultiplierMax + 1);
                    rolledDamages[i] = handCount * multiplier;
                }
            }
        }

        /// <summary>
        /// 実効確率を取得（BaseProbability + BonusProbability、上限1.0）
        /// </summary>
        public float GetEffectiveProbability(int effectIndex)
            => Mathf.Min(rolledProbabilities[effectIndex] + bonusProbabilities[effectIndex], 1f);
        
        /// <summary>
        /// 実効ダメージを取得（BaseDamage + BonusDamage、上限なし）
        /// </summary>
        public int GetEffectiveDamage(int effectIndex)
            => rolledDamages[effectIndex] + bonusDamages[effectIndex];

        /// <summary>
        /// 実効軽減率（%）を取得。DefenseEffect 専用。
        /// rolledDamages[] に軽減率を格納しているため同じ経路で取得する。
        /// </summary>
        public int GetEffectiveReductionRate(int effectIndex)
            => Mathf.Clamp(rolledDamages[effectIndex] + bonusDamages[effectIndex], 0, 100);

        /// <summary>
        /// ロール済み基礎値を取得
        /// </summary>
        public float GetBaseProbability(int effectIndex) => rolledProbabilities[effectIndex];
        public int GetBaseDamage(int effectIndex) => rolledDamages[effectIndex];

        /// <summary>
        /// ボーナス値を取得
        /// </summary>
        public float GetBonusProbability(int effectIndex) => bonusProbabilities[effectIndex];
        public int GetBonusDamage(int effectIndex) => bonusDamages[effectIndex];

        /// <summary>
        /// ボーナス値を設定（上書き）
        /// </summary>
        public void SetBonusProbability(int effectIndex, float bonus)
            => bonusProbabilities[effectIndex] = bonus;

        public void SetBonusDamage(int effectIndex, int bonus)
            => bonusDamages[effectIndex] = bonus;

        /// <summary>
        /// ボーナス値を加算
        /// </summary>
        public void AddBonusProbability(int effectIndex, float bonus)
            => bonusProbabilities[effectIndex] += bonus;

        public void AddBonusDamage(int effectIndex, int bonus) 
            => bonusDamages[effectIndex] += bonus;

        /// <summary>
        /// 実効確率で判定。成功時に true を返す。
        /// </summary>
        public bool TryExecuteEffect(int effectIndex) 
            => Random.value <= GetEffectiveProbability(effectIndex);

        /// <summary>
        /// 指定インデックスの効果オブジェクトを取得
        /// </summary>
        public T GetEffect<T>(int effectIndex) where T : CardEffectBase
        {
            if (effectIndex >= 0 && effectIndex < OriginalData.Effects.Count)
                if (OriginalData.Effects[effectIndex] is T typed)
                    return typed;
            return null;
        }

        /// <summary>
        /// ロール履歴をリセット（プール再利用時など）
        /// </summary>
        public void Reset()
        {
            IsRolled = false;
            System.Array.Clear(rolledProbabilities, 0, rolledProbabilities.Length);
            System.Array.Clear(rolledDamages, 0, rolledDamages.Length);
            System.Array.Clear(bonusProbabilities, 0, bonusProbabilities.Length);
            System.Array.Clear(bonusDamages, 0, bonusDamages.Length);
        }
    }
}