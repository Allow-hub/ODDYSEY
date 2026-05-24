using System.Collections.Generic;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// バトル報酬のカード候補リスト。
    /// StageNodeData に持たせて、バトルごとに異なる報酬プールを設定する。
    ///
    /// 使い方：
    ///   Create > ODDESEY > BattleRewardData で作成
    ///   StageNodeData.RewardData にアサイン
    /// </summary>
    [CreateAssetMenu(fileName = "BattleRewardData", menuName = "ODDESEY/BattleRewardData")]
    public class BattleRewardData : ScriptableObject
    {
        [Tooltip("このバトルで報酬として選べるカードのプール")]
        public List<CardData> CardPool = new();

        [Tooltip("プレイヤーに提示する候補枚数")]
        [Range(1, 5)]
        public int OfferCount = 3;

        /// <summary>
        /// CardPool からランダムに OfferCount 枚を重複なしで抽選して返す。
        /// </summary>
        public List<CardData> DrawOffers()
        {
            if (CardPool == null || CardPool.Count == 0) return new List<CardData>();

            var pool   = new List<CardData>(CardPool);
            var result = new List<CardData>();
            int take   = Mathf.Min(OfferCount, pool.Count);

            for (int i = 0; i < take; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }
    }
}