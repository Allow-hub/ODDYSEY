using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ステージマップの管理。
    /// ノード選択UI・進行状態を担当し、次フェーズを MainManager に通知する。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("ノードViewのリスト（上から順、数はStageMapDataのnodes数に合わせる）")]
        [SerializeField] private List<MapNodeView> nodeViews;

        [Header("選択肢ボタンのPrefab（NodeChoiceButton がアタッチされていること）")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("ラッキーゲージ")]
        [SerializeField] private LuckGaugeView luckGaugeView;

        // -------------------------------------------------------
        // Events
        // -------------------------------------------------------
        public event Action OnBattleRequested;
        public event Action OnEventRequested;
        public event Action OnStageCompleted;

        // -------------------------------------------------------
        // 内部状態
        // -------------------------------------------------------
        private StageMapData mapData;
        private MapProgressState progressState;

        // -------------------------------------------------------
        // 初期化
        // -------------------------------------------------------

        /// <summary>
        /// MainManager からステージ定義と進行状態を渡して初期化する。
        /// </summary>
        public void Initialize(StageMapData data, MapProgressState progress)
        {
            mapData = data;
            progressState = progress;

            luckGaugeView.Setup(100f);
            luckGaugeView.UpdateGaugeImmediate(MainManager.I?.LuckGaugeValue ?? 0f, 100f, false);
            RefreshView();
        }

        // -------------------------------------------------------
        // ビュー更新
        // -------------------------------------------------------

        private void RefreshView()
        {
            if (mapData == null) return;

            int current = progressState.currentNodeIndex;

            for (int i = 0; i < nodeViews.Count; i++)
            {
                if (i >= mapData.nodes.Count)
                {
                    nodeViews[i].gameObject.SetActive(false);
                    continue;
                }

                nodeViews[i].gameObject.SetActive(true);

                MapNodeView.NodeState state;
                if (i < current) state = MapNodeView.NodeState.Cleared;
                else if (i == current) state = MapNodeView.NodeState.Active;
                else state = MapNodeView.NodeState.Locked;

                // buttonPrefab を渡してノードView内で Instantiate させる
                nodeViews[i].Setup(mapData.nodes[i], state, choiceButtonPrefab, OnNodeChoiceSelected);
            }
        }

        // -------------------------------------------------------
        // 選択処理
        // -------------------------------------------------------

        private void OnNodeChoiceSelected(NodeType chosenType)
        {
            progressState.Advance();

            if (progressState.IsCompleted(mapData.nodes.Count))
            {
                OnStageCompleted?.Invoke();
                return;
            }

            switch (chosenType)
            {
                case NodeType.Battle: OnBattleRequested?.Invoke(); break;
                case NodeType.Event: OnEventRequested?.Invoke(); break;
                case NodeType.Rest: OnBattleRequested?.Invoke(); break; // 必要なら拡張
            }
        }
    }
}