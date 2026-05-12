using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// バトル勝利後のカード選択画面を管理する。
    ///
    /// 責務：
    ///   - MainManager から報酬データを受け取り候補を抽選
    ///   - CardRewardView に候補を渡して表示
    ///   - プレイヤーが選んだカードを GameContext に追加
    ///   - 完了を MainManager に通知
    /// </summary>
    public class CardRewardController : MonoBehaviour
    {
        public event Action OnRewardCompleted;

        [SerializeField] private CardRewardView rewardView;

        public void Initialize(BattleRewardData rewardData)
        {
            if (rewardData == null || rewardData.CardPool.Count == 0)
            {
                // 報酬なし → 即完了
                OnRewardCompleted?.Invoke();
                return;
            }

            var offers = rewardData.DrawOffers();
            rewardView.Setup(offers, OnCardSelected, OnSkipped);
        }

        private void OnCardSelected(CardData card)
        {
            MainManager.I?.GameContext?.AddCard(card);
            OnRewardCompleted?.Invoke();
        }

        private void OnSkipped()
        {
            // スキップ：何も追加せず完了
            OnRewardCompleted?.Invoke();
        }
    }
}