using System;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY.Battle;
using TechC.ODDESEY.Event;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// ステージマップの管理。
    ///
    /// 変更点：
    ///   - OnEventRequested を Action → Action<EventData> に変更。
    ///     MainManager がどの EventData でイベントを開くか判断できるようにする。
    ///   - OnNodeChoiceSelected で選択されたノードの EventData を取得して渡す。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [Header("ノードViewのリスト（上から順、数はStageMapDataのnodes数に合わせる）")]
        [SerializeField] private List<MapNodeView> nodeViews;

        [Header("選択肢ボタンのPrefab")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("ラッキーゲージ")]
        [SerializeField] private LuckGaugeView luckGaugeView;

        // ─── Events ───────────────────────────────────────────────────────
        public event Action OnBattleRequested;
        public event Action<EventData> OnEventRequested; // ← EventData を渡すように変更
        public event Action OnStageCompleted;

        // ─── 内部状態 ────────────────────────────────────────────────────
        private StageMapData mapData;
        private MapProgressState progressState;

        // ─── 初期化 ───────────────────────────────────────────────────────

        public void Initialize(StageMapData data, MapProgressState progress)
        {
            mapData       = data;
            progressState = progress;

            luckGaugeView.Setup(MainManager.I.LuckGaugeMax);
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
                if (i < current)      state = MapNodeView.NodeState.Cleared;
                else if (i == current) state = MapNodeView.NodeState.Active;
                else                   state = MapNodeView.NodeState.Locked;

                nodeViews[i].Setup(mapData.nodes[i], state, choiceButtonPrefab, OnNodeChoiceSelected);
            }
        }

        // ─── 選択処理 ────────────────────────────────────────────────────

        private void OnNodeChoiceSelected(NodeType chosenType)
        {
            // 選択時点のノードインデックスを保持してから進める
            int selectedNodeIndex = progressState.currentNodeIndex;
            progressState.Advance();

            if (progressState.IsCompleted(mapData.nodes.Count))
            {
                OnStageCompleted?.Invoke();
                return;
            }

            switch (chosenType)
            {
                case NodeType.Battle:
                    OnBattleRequested?.Invoke();
                    break;

                case NodeType.Event:
                    // 選択されたノードの EventData を取得して渡す
                    var eventData = mapData.nodes[selectedNodeIndex].EventData;
                    if (eventData == null)
                        Debug.LogWarning($"[MapController] nodes[{selectedNodeIndex}] に EventData がアサインされていません。");
                    OnEventRequested?.Invoke(eventData);
                    break;

                case NodeType.Rest:
                    OnBattleRequested?.Invoke(); // 必要なら拡張
                    break;
            }
        }
    }
}