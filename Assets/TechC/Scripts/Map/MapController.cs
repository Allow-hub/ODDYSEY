using System;
using System.Collections;
using System.Collections.Generic;
using TechC.Core.Manager;
using TechC.ODDESEY;
using TechC.ODDESEY.Battle;
using TechC.ODDESEY.Event;
using TechC.ODDESEY.Reward;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// Owns the map-selection screen.
    /// Map nodes are visual state only; actual selection is done by bottom choice buttons.
    /// The map layout is rebuilt from StageMapData so node count and branches can change with data.
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [Header("Node Views")]
        [SerializeField] private MapNodeView nodeViewPrefab;
        [SerializeField] private RectTransform nodeContainer;
        [SerializeField] private List<MapNodeView> nodeViews;
        [SerializeField] private Vector2 nodeGap = new(220f, 160f);
        [SerializeField] private Vector2 nodeSize = new(100f, 100f);
        [SerializeField] private Vector2 nodeContainerPadding = new(240f, 120f);

        [Header("Map Scroll")]
        [SerializeField] private ScrollRect mapScrollRect;
        [SerializeField] private bool createScrollRectIfMissing = true;
        [SerializeField] private Vector2 mapViewportSize = new(1180f, 440f);
        [SerializeField] private Vector2 mapViewportPosition = new(0f, 70f);
        [SerializeField] private bool centerCurrentNodeOnRefresh = true;
        [SerializeField] private bool animateCurrentNodeScroll = true;
        [SerializeField, Min(0f)] private float currentNodeScrollDuration = 0.35f;

        [Header("Choice Buttons")]
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Transform choiceButtonContainer;

        [Header("Luck Gauge")]
        [SerializeField] private LuckGaugeView luckGaugeView;

        [Header("Player Status")]
        [SerializeField] private HpView playerHpView;

        [Header("Debug")]
        [SerializeField, Min(0)] private int debugStartNodeIndex;

        public event Action<BattleRewardData, bool, EnemyData> OnBattleRequested;
        public event Action<EventData> OnEventRequested;
        public event Action OnStageCompleted;

        private readonly List<NodeChoiceButton> spawnedChoiceButtons = new();
        private readonly List<MapNodeView> spawnedNodeViews = new();
        private StageMapData mapData;
        private MapProgressState progressState;
        private Coroutine currentNodeScrollCoroutine;

        public void Initialize(StageMapData data, MapProgressState progress)
        {
            mapData = data;
            progressState = progress;

            if (debugStartNodeIndex > 0 && mapData != null && progressState != null)
            {
                progressState.currentNodeIndex = Mathf.Clamp(debugStartNodeIndex, 0, Mathf.Max(0, mapData.nodes.Count - 1));
            }

            if (luckGaugeView != null)
            {
                luckGaugeView.Setup(100f);
                luckGaugeView.UpdateGaugeImmediate(MainManager.I?.LuckGaugeValue ?? 0f, 100f, false);
            }

            RefreshPlayerHpView();
            RebuildNodeViews();
            RefreshView();
        }

        private void RefreshPlayerHpView()
        {
            if (playerHpView == null)
            {
                return;
            }

            GameContext context = MainManager.I?.GameContext;
            int maxHp = context != null ? context.PlayerHpMax : 100;
            int currentHp = context != null ? context.PlayerHp : maxHp;

            playerHpView.Setup(maxHp);
            playerHpView.UpdateImmediate(currentHp, maxHp);
        }

        private void RefreshView()
        {
            if (mapData == null)
            {
                return;
            }

            ClearChoiceButtons();

            if (progressState == null)
            {
                Debug.LogError("[MapController] progressState is null.");
                return;
            }

            int current = progressState.currentNodeIndex;
            List<int> selectableNodeIndices = GetSelectableNodeIndices(current);

            // Visible nodes are generated from a single hidden template, so designers
            // do not need to keep prefab children in sync with StageMapData.
            if (spawnedNodeViews.Count != mapData.nodes.Count)
            {
                RebuildNodeViews();
            }

            if (spawnedNodeViews.Count == 0 && mapData.nodes.Count > 0)
            {
                Debug.LogError("[MapController] NodeView instances were not created.");
                return;
            }

            for (int i = 0; i < spawnedNodeViews.Count; i++)
            {
                if (i >= mapData.nodes.Count)
                {
                    spawnedNodeViews[i].gameObject.SetActive(false);
                    continue;
                }

                spawnedNodeViews[i].gameObject.SetActive(true);

                MapNodeView.NodeState state;
                if (i == current) state = MapNodeView.NodeState.Current;
                else if (selectableNodeIndices.Contains(i)) state = MapNodeView.NodeState.Active;
                else if (progressState.HasVisited(i)) state = MapNodeView.NodeState.Cleared;
                else state = MapNodeView.NodeState.Locked;

                spawnedNodeViews[i].Setup(mapData.nodes[i], state);
            }

            RefreshChoiceButtons(selectableNodeIndices);
            CenterCurrentNodeIfNeeded(current);
        }

        private List<int> GetSelectableNodeIndices(int current)
        {
            return GetNextNodeIndices(current);
        }

        private List<int> GetNextNodeIndices(int current)
        {
            List<int> result = new();

            if (mapData == null || current < 0 || current >= mapData.nodes.Count)
            {
                return result;
            }

            StageNodeData currentNode = mapData.nodes[current];
            // nextNodeIndices is the graph edge list for the map. This replaces the
            // old "choices are node types" model so branching can point at concrete nodes.
            if (currentNode.nextNodeIndices != null && currentNode.nextNodeIndices.Count > 0)
            {
                foreach (int nodeIndex in currentNode.nextNodeIndices)
                {
                    if (nodeIndex >= 0 && nodeIndex < mapData.nodes.Count && !result.Contains(nodeIndex))
                    {
                        result.Add(nodeIndex);
                    }
                }

                return result;
            }

            int nextIndex = current + 1;
            if (nextIndex < mapData.nodes.Count)
            {
                result.Add(nextIndex);
            }

            return result;
        }

        private void RebuildNodeViews()
        {
            if (mapData == null)
            {
                return;
            }

            MapNodeView template = ResolveNodeViewTemplate();
            RectTransform parent = ResolveNodeContainer(template);

            if (template == null || parent == null)
            {
                Debug.LogError("[MapController] NodeView template or nodeContainer is not assigned.");
                return;
            }

            ResolveMapScrollRect(parent);
            ClearSpawnedNodeViews();
            template.gameObject.SetActive(false);

            for (int i = 0; i < mapData.nodes.Count; i++)
            {
                MapNodeView view = Instantiate(template, parent);
                view.name = $"NodeView ({i})";
                view.gameObject.SetActive(true);
                spawnedNodeViews.Add(view);
            }

            LayoutNodeViews();
        }

        private MapNodeView ResolveNodeViewTemplate()
        {
            if (nodeViewPrefab != null)
            {
                return nodeViewPrefab;
            }

            if (nodeViews != null)
            {
                foreach (MapNodeView view in nodeViews)
                {
                    if (view != null)
                    {
                        return view;
                    }
                }
            }

            if (nodeContainer != null)
            {
                return nodeContainer.GetComponentInChildren<MapNodeView>(includeInactive: true);
            }

            return GetComponentInChildren<MapNodeView>(includeInactive: true);
        }

        private RectTransform ResolveNodeContainer(MapNodeView template)
        {
            if (nodeContainer != null)
            {
                return nodeContainer;
            }

            if (template != null && template.transform.parent is RectTransform parent)
            {
                nodeContainer = parent;
                return nodeContainer;
            }

            Canvas canvas = GetComponentInChildren<Canvas>(includeInactive: true);
            if (canvas == null)
            {
                return null;
            }

            GameObject container = new("NodeContainer");
            RectTransform rectTransform = container.AddComponent<RectTransform>();
            rectTransform.SetParent(canvas.transform, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 70f);
            rectTransform.sizeDelta = new Vector2(1240f, 360f);
            nodeContainer = rectTransform;
            return nodeContainer;
        }

        private void ResolveMapScrollRect(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            if (mapScrollRect == null)
            {
                mapScrollRect = content.GetComponentInParent<ScrollRect>();
            }

            if (mapScrollRect != null)
            {
                ConfigureScrollRect(mapScrollRect, content);
                return;
            }

            if (!createScrollRectIfMissing)
            {
                return;
            }

            // Compatibility path for old work scenes. The intended prefab setup is a
            // permanent ScrollRect created by MapPrefabConfigurator.
            RectTransform originalParent = content.parent as RectTransform;
            if (originalParent == null)
            {
                return;
            }

            int siblingIndex = content.GetSiblingIndex();
            Vector2 originalAnchoredPosition = content.anchoredPosition;

            GameObject scrollViewObject = new("MapScrollView");
            RectTransform scrollView = scrollViewObject.AddComponent<RectTransform>();
            scrollView.SetParent(originalParent, false);
            scrollView.SetSiblingIndex(siblingIndex);
            scrollView.anchorMin = new Vector2(0.5f, 0.5f);
            scrollView.anchorMax = new Vector2(0.5f, 0.5f);
            scrollView.pivot = new Vector2(0.5f, 0.5f);
            scrollView.anchoredPosition = originalAnchoredPosition != Vector2.zero
                ? originalAnchoredPosition
                : mapViewportPosition;
            scrollView.sizeDelta = mapViewportSize;

            Image raycastImage = scrollViewObject.AddComponent<Image>();
            raycastImage.color = new Color(1f, 1f, 1f, 0f);
            raycastImage.raycastTarget = true;

            scrollViewObject.AddComponent<RectMask2D>();
            mapScrollRect = scrollViewObject.AddComponent<ScrollRect>();

            content.SetParent(scrollView, false);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;

            ConfigureScrollRect(mapScrollRect, content);
        }

        private static void ConfigureScrollRect(ScrollRect scrollRect, RectTransform content)
        {
            scrollRect.content = content;
            scrollRect.viewport = scrollRect.transform as RectTransform;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;
        }

        private void LayoutNodeViews()
        {
            if (mapData == null || spawnedNodeViews.Count == 0)
            {
                return;
            }

            int[] columns = CalculateNodeColumns();
            Dictionary<int, List<int>> nodesByColumn = new();
            int maxColumn = 0;
            float maxAbsY = 0f;

            for (int i = 0; i < mapData.nodes.Count; i++)
            {
                int column = columns[i];
                maxColumn = Mathf.Max(maxColumn, column);

                if (!nodesByColumn.TryGetValue(column, out List<int> columnNodes))
                {
                    columnNodes = new List<int>();
                    nodesByColumn.Add(column, columnNodes);
                }

                columnNodes.Add(i);
            }

            float centerColumn = maxColumn * 0.5f;
            foreach (KeyValuePair<int, List<int>> pair in nodesByColumn)
            {
                List<int> columnNodes = pair.Value;
                float x = (pair.Key - centerColumn) * nodeGap.x;

                for (int order = 0; order < columnNodes.Count; order++)
                {
                    int nodeIndex = columnNodes[order];
                    if (nodeIndex >= spawnedNodeViews.Count || spawnedNodeViews[nodeIndex] == null)
                    {
                        continue;
                    }

                    // Branches in the same progression step are stacked vertically
                    // around the normal baseline, avoiding overlap without shrinking nodes.
                    float y = ((columnNodes.Count - 1) * 0.5f - order) * nodeGap.y;
                    maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(y));

                    RectTransform rectTransform = spawnedNodeViews[nodeIndex].transform as RectTransform;
                    if (rectTransform == null)
                    {
                        continue;
                    }

                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = new Vector2(x, y);
                    rectTransform.sizeDelta = nodeSize;
                }
            }

            if (nodeContainer != null)
            {
                float width = maxColumn * nodeGap.x + nodeSize.x + nodeContainerPadding.x;
                float height = maxAbsY * 2f + nodeSize.y + nodeContainerPadding.y;
                nodeContainer.sizeDelta = new Vector2(
                    Mathf.Max(width, mapViewportSize.x),
                    Mathf.Max(height, mapViewportSize.y));
            }
        }

        private int[] CalculateNodeColumns()
        {
            int count = mapData.nodes.Count;
            int[] columns = new int[count];
            for (int i = 0; i < count; i++)
            {
                columns[i] = -1;
            }

            if (count == 0)
            {
                return columns;
            }

            Queue<int> queue = new();
            columns[0] = 0;
            queue.Enqueue(0);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int nextColumn = columns[current] + 1;
                List<int> nextIndices = GetNextNodeIndices(current);

                foreach (int nextIndex in nextIndices)
                {
                    if (nextIndex < 0 || nextIndex >= count)
                    {
                        continue;
                    }

                    if (columns[nextIndex] == -1 || nextColumn < columns[nextIndex])
                    {
                        columns[nextIndex] = nextColumn;
                        queue.Enqueue(nextIndex);
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (columns[i] >= 0)
                {
                    continue;
                }

                columns[i] = i > 0 ? columns[i - 1] + 1 : 0;
            }

            return columns;
        }

        private void CenterCurrentNodeIfNeeded(int current)
        {
            if (!centerCurrentNodeOnRefresh || mapScrollRect == null || mapScrollRect.content == null)
            {
                return;
            }

            if (current < 0 || current >= spawnedNodeViews.Count || spawnedNodeViews[current] == null)
            {
                return;
            }

            RectTransform viewport = mapScrollRect.viewport != null
                ? mapScrollRect.viewport
                : mapScrollRect.transform as RectTransform;
            RectTransform content = mapScrollRect.content;
            RectTransform currentNode = spawnedNodeViews[current].transform as RectTransform;

            if (viewport == null || content == null || currentNode == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float scrollableWidth = content.rect.width - viewport.rect.width;
            if (scrollableWidth <= 0f)
            {
                ApplyHorizontalScrollPosition(0f);
                return;
            }

            float targetCenterFromLeft = currentNode.anchoredPosition.x + content.pivot.x * content.rect.width;
            float targetScroll = Mathf.Clamp(targetCenterFromLeft - viewport.rect.width * 0.5f, 0f, scrollableWidth);
            ApplyHorizontalScrollPosition(targetScroll / scrollableWidth);
        }

        private void ApplyHorizontalScrollPosition(float normalizedPosition)
        {
            float target = Mathf.Clamp01(normalizedPosition);

            if (currentNodeScrollCoroutine != null)
            {
                StopCoroutine(currentNodeScrollCoroutine);
                currentNodeScrollCoroutine = null;
            }

            if (!Application.isPlaying || !animateCurrentNodeScroll || currentNodeScrollDuration <= 0f)
            {
                mapScrollRect.horizontalNormalizedPosition = target;
                return;
            }

            currentNodeScrollCoroutine = StartCoroutine(AnimateHorizontalScroll(target));
        }

        private IEnumerator AnimateHorizontalScroll(float target)
        {
            float start = mapScrollRect.horizontalNormalizedPosition;
            if (Mathf.Approximately(start, target))
            {
                mapScrollRect.horizontalNormalizedPosition = target;
                currentNodeScrollCoroutine = null;
                yield break;
            }

            float duration = Mathf.Max(0.01f, currentNodeScrollDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (mapScrollRect == null)
                {
                    currentNodeScrollCoroutine = null;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                mapScrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, easedT);
                yield return null;
            }

            mapScrollRect.horizontalNormalizedPosition = target;
            currentNodeScrollCoroutine = null;
        }

        private void RefreshChoiceButtons(List<int> selectableNodeIndices)
        {
            if (selectableNodeIndices.Count == 0)
            {
                return;
            }

            if (choiceButtonPrefab == null)
            {
                Debug.LogError("[MapController] choiceButtonPrefab is null.");
                return;
            }

            Transform parent = ResolveChoiceButtonContainer();

            foreach (int nodeIndex in selectableNodeIndices)
            {
                StageNodeData node = mapData.nodes[nodeIndex];
                GameObject go = Instantiate(choiceButtonPrefab, parent);
                NodeChoiceButton btn = go.GetComponentInChildren<NodeChoiceButton>(includeInactive: true);

                if (btn == null)
                {
                    Debug.LogError($"[MapController] NodeChoiceButton was not found in '{go.name}'.");
                    Destroy(go);
                    continue;
                }

                EventMapIconType eventIconType = node.EventData != null
                    ? node.EventData.MapIconType
                    : EventMapIconType.Risk;

                // Buttons select concrete next-node indices. Their visuals are button UI,
                // independent from map node tile/icon visuals.
                int capturedNodeIndex = nodeIndex;
                btn.Setup(node.nodeType, eventIconType, () => OnNodeChoiceSelected(capturedNodeIndex));
                btn.SetInteractable(true);
                spawnedChoiceButtons.Add(btn);
            }
        }

        private Transform ResolveChoiceButtonContainer()
        {
            if (choiceButtonContainer != null)
            {
                return choiceButtonContainer;
            }

            Canvas canvas = GetComponentInChildren<Canvas>(includeInactive: true);
            if (canvas == null)
            {
                return transform;
            }

            GameObject container = new("ChoiceButtonContainer");
            RectTransform rectTransform = container.AddComponent<RectTransform>();
            rectTransform.SetParent(canvas.transform, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 137f);
            // 最大2択を横並びにしても、大きな操作ボタン同士が重ならない領域を確保する。
            rectTransform.sizeDelta = new Vector2(720f, 120f);

            HorizontalLayoutGroup layoutGroup = container.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 40f;

            choiceButtonContainer = rectTransform;
            return choiceButtonContainer;
        }

        private void OnNodeChoiceSelected(int selectedIndex)
        {
            if (mapData == null || progressState == null)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= mapData.nodes.Count)
            {
                Debug.LogWarning($"[MapController] selectedIndex is out of range: {selectedIndex}");
                return;
            }

            List<int> selectableNodeIndices = GetSelectableNodeIndices(progressState.currentNodeIndex);
            if (!selectableNodeIndices.Contains(selectedIndex))
            {
                Debug.LogWarning($"[MapController] selectedIndex is not selectable: {selectedIndex}");
                return;
            }

            progressState.MoveTo(selectedIndex);
            StageNodeData node = mapData.nodes[selectedIndex];
            bool isTerminalNode = GetNextNodeIndices(selectedIndex).Count == 0;

            switch (node.nodeType)
            {
                case NodeType.Battle:
                    bool isBoss = node.IsBossNode || isTerminalNode;
                    OnBattleRequested?.Invoke(node.RewardData, isBoss, node.EnemyData);
                    break;

                case NodeType.Event:
                    if (node.EventData == null)
                    {
                        Debug.LogWarning($"[MapController] nodes[{selectedIndex}] has no EventData.");
                    }

                    OnEventRequested?.Invoke(node.EventData);
                    if (isTerminalNode) OnStageCompleted?.Invoke();
                    break;

                case NodeType.Rest:
                    if (isTerminalNode)
                    {
                        OnStageCompleted?.Invoke();
                    }
                    else
                    {
                        RefreshView();
                    }
                    break;
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (NodeChoiceButton btn in spawnedChoiceButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }

            spawnedChoiceButtons.Clear();
        }

        private void ClearSpawnedNodeViews()
        {
            foreach (MapNodeView view in spawnedNodeViews)
            {
                if (view != null)
                {
                    DestroyNodeView(view.gameObject);
                }
            }

            spawnedNodeViews.Clear();
        }

        private static void DestroyNodeView(GameObject nodeViewObject)
        {
            if (Application.isPlaying)
            {
                Destroy(nodeViewObject);
            }
            else
            {
                DestroyImmediate(nodeViewObject);
            }
        }

        private void OnDestroy()
        {
            if (currentNodeScrollCoroutine != null)
            {
                StopCoroutine(currentNodeScrollCoroutine);
                currentNodeScrollCoroutine = null;
            }

            ClearChoiceButtons();
            ClearSpawnedNodeViews();
        }
    }
}
