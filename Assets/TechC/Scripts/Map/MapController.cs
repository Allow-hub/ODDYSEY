using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Battle;
using TechC.ODDESEY.Event;
using TechC.ODDESEY.Reward;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ステージマップの管理。
    ///
    /// 変更点：
    ///   - OnBattleRequested を Action<BattleRewardData, bool> に変更。
    ///     RewardData（報酬候補）と IsBossNode（ボスフラグ）を MainManager に渡す。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [Header("ノードViewのリスト")]
        [SerializeField] private List<MapNodeView> nodeViews;

        [Header("選択肢ボタンのPrefab")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("ラッキーゲージ")]
        [SerializeField] private LuckGaugeView luckGaugeView;

        // ─── Events ───────────────────────────────────────────────────────
        /// <summary>RewardData・ボスフラグを含むバトル開始通知</summary>
        public event Action<BattleRewardData, bool> OnBattleRequested;
        public event Action<EventData> OnEventRequested;
        public event Action OnStageCompleted;

        // ─── 内部状態 ────────────────────────────────────────────────────
        private StageMapData mapData;
        private MapProgressState progressState;

        // ─── 初期化 ───────────────────────────────────────────────────────

        public void Initialize(StageMapData data, MapProgressState progress)
        {
            mapData = data;
            progressState = progress;

            luckGaugeView.Setup(100f);
            luckGaugeView.UpdateGaugeImmediate(MainManager.I?.LuckGaugeValue ?? 0f, 100f, false);
            RefreshView();
        }

        // ─── ビュー更新 ──────────────────────────────────────────────────

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

                nodeViews[i].Setup(mapData.nodes[i], state, choiceButtonPrefab, OnNodeChoiceSelected);
            }
        }

        // ─── 選択処理 ────────────────────────────────────────────────────

        private void OnNodeChoiceSelected(NodeType chosenType)
        {
            int selectedIndex = progressState.currentNodeIndex;
            progressState.Advance();

            if (progressState.IsCompleted(mapData.nodes.Count))
            {
                OnStageCompleted?.Invoke();
                return;
            }

            var node = mapData.nodes[selectedIndex];

            switch (chosenType)
            {
                case NodeType.Battle:
                    // RewardData と IsBossNode を渡す
                    OnBattleRequested?.Invoke(node.RewardData, node.IsBossNode);
                    break;

                case NodeType.Event:
                    if (node.EventData == null)
                        Debug.LogWarning($"[MapController] nodes[{selectedIndex}] に EventData がアサインされていません。");
                    OnEventRequested?.Invoke(node.EventData);
                    break;

                case NodeType.Rest:
                    OnBattleRequested?.Invoke(null, false);
                    break;
            }
        }
    }
}