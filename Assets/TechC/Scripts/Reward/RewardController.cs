using System;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// リザルト・報酬画面の管理。
    /// 報酬選択が完了したら MainManager に通知する。
    /// </summary>
    public class RewardController : MonoBehaviour
    {
        // -------------------------------------------------------
        // MainManager へ通知するイベント
        // -------------------------------------------------------

        public event Action OnResultClosed;

        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        [Header("UI")]
        [SerializeField] private GameObject rewardUI;

        // -------------------------------------------------------
        // 初期化（MainManager から呼ばれる）
        // -------------------------------------------------------

        public void Initialize()
        {
            // TODO: 勝敗フラグを PlayerData から取得して表示を切り替える

            if (rewardUI != null)
                rewardUI.SetActive(true);

            // TODO: 報酬カード候補を生成して表示する
        }

        // -------------------------------------------------------
        // 報酬選択（UIボタンから呼ぶ）
        // -------------------------------------------------------

        /// <summary>報酬カードを選択した</summary>
        public void SelectReward(int rewardIndex)
        {
            // TODO: 選択した報酬を PlayerData のデッキに追加する
            CloseResult();
        }

        /// <summary>報酬をスキップした</summary>
        public void SkipReward()
        {
            CloseResult();
        }

        private void CloseResult()
        {
            if (rewardUI != null)
                rewardUI.SetActive(false);

            OnResultClosed?.Invoke();
        }
    }
}