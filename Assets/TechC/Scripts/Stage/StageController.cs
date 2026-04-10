using System;
using UnityEngine;

namespace TechC.ODDESEY.Stage
{
    /// <summary>
    /// ステージマップの管理。
    /// ノード選択UI・進行状態を担当し、次フェーズを MainManager に通知する。
    /// </summary>
    public class StageController : MonoBehaviour
    {
        // -------------------------------------------------------
        // MainManager へ通知するイベント
        // -------------------------------------------------------

        /// <summary>ノードを選択 → バトルへ</summary>
        public event Action OnBattleRequested;

        /// <summary>ノードを選択 → イベントへ</summary>
        public event Action OnEventRequested;

        /// <summary>ステージクリア（ボス討伐後など）</summary>
        public event Action OnStageCompleted;

        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        [Header("Node UI")]
        [SerializeField] private GameObject nodeSelectUI;   // ノード選択パネル

        // -------------------------------------------------------
        // 内部状態
        // -------------------------------------------------------

        // TODO: マップデータ（ノード一覧）を保持する
        // private StageMapData mapData;

        // -------------------------------------------------------
        // 初期化（MainManager から呼ばれる）
        // -------------------------------------------------------

        public void Initialize()
        {
            ShowMap();
        }

        // -------------------------------------------------------
        // マップ表示
        // -------------------------------------------------------

        private void ShowMap()
        {
            if (nodeSelectUI != null)
                nodeSelectUI.SetActive(true);

            // TODO: マップデータを元にノードを生成・表示する
        }

        // -------------------------------------------------------
        // ノード選択（UIボタンなどから呼ぶ）
        // -------------------------------------------------------

        /// <summary>バトルノードを選択した</summary>
        public void SelectBattleNode()
        {
            // TODO: 選択したノードの情報を PlayerData / BattleData に渡す
            OnBattleRequested?.Invoke();
        }

        /// <summary>イベントノードを選択した</summary>
        public void SelectEventNode()
        {
            // TODO: 選択したノードのイベント種別を PlayerData / EventData に渡す
            OnEventRequested?.Invoke();
        }

        /// <summary>ステージの最終ノードを突破した</summary>
        public void CompleteStage()
        {
            OnStageCompleted?.Invoke();
        }
    }
}