using System;
using UnityEngine;

namespace TechC.ODDESEY.Event
{
    /// <summary>
    /// イベントノードの管理。
    /// 選択肢の表示・結果適用を担当し、完了を MainManager に通知する。
    /// </summary>
    public class EventController : MonoBehaviour
    {
        // -------------------------------------------------------
        // MainManager へ通知するイベント
        // -------------------------------------------------------

        public event Action OnEventCompleted;

        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        [Header("UI")]
        [SerializeField] private GameObject eventUI;

        // -------------------------------------------------------
        // 初期化（MainManager から呼ばれる）
        // -------------------------------------------------------

        public void Initialize()
        {
            if (eventUI != null)
                eventUI.SetActive(true);

            // TODO: PlayerData から現在のノード情報を取得してイベント内容をセット
            // TODO: 選択肢ボタンを動的に生成する
        }

        // -------------------------------------------------------
        // 選択肢（UIボタンから呼ぶ）
        // -------------------------------------------------------

        /// <summary>選択肢インデックスを受け取り結果を適用する</summary>
        public void SelectChoice(int choiceIndex)
        {
            // TODO: 選択肢に応じて PlayerData を変更（HP増減・カード獲得など）
            CompleteEvent();
        }

        private void CompleteEvent()
        {
            if (eventUI != null)
                eventUI.SetActive(false);

            OnEventCompleted?.Invoke();
        }
    }
}