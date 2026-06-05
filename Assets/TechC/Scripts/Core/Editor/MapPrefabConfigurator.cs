using TechC.ODDESEY.Battle;
using TechC.ODDESEY.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.Core.Editor
{
    public static class MapPrefabConfigurator
    {
        private const string MapPrefabPath = "Assets/TechC/Prefabs/Map/Map.prefab";
        private const string NodeViewPrefabPath = "Assets/TechC/Prefabs/Map/NodeView.prefab";
        private const string PlayerHpPrefabPath = "Assets/TechC/Prefabs/UI/PlayerHpSlider.prefab";
        private static readonly Vector2 ViewportSize = new(1180f, 440f);
        private static readonly Vector2 ViewportPosition = new(0f, 70f);
        private static readonly Vector2 NodeContentSize = new(1240f, 440f);
        private static readonly Vector2 CurrentMarkerSize = new(100f, 100f);
        private static readonly Vector2 CurrentMarkerOffset = new(0f, -12f);

        [MenuItem("Tools/TechC/Map/Configure Map Prefab")]
        public static void ConfigureMapPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"Map prefab not found: {MapPrefabPath}");
                return;
            }

            try
            {
                MapController controller = prefabRoot.GetComponent<MapController>();
                Canvas canvas = prefabRoot.GetComponentInChildren<Canvas>(includeInactive: true);
                RectTransform nodeContent = FindRectTransform(prefabRoot, "Node");

                if (controller == null || canvas == null || nodeContent == null)
                {
                    Debug.LogError("[MapPrefabConfigurator] MapController, Canvas, or Node content was not found.");
                    return;
                }

                RectTransform scrollView = EnsureScrollView(canvas.transform, nodeContent);
                ConfigureViewport(scrollView);
                ConfigureNodeContent(scrollView, nodeContent);
                ScrollRect scrollRect = ConfigureScrollRect(scrollView, nodeContent);
                MapNodeView nodeTemplate = ConfigureNodeTemplate(nodeContent);
                HpView playerHpView = ConfigurePlayerHpView(canvas.transform);

                ConfigureController(controller, nodeContent, scrollRect, nodeTemplate, playerHpView);
                ConfigureCurrentMarkers(prefabRoot);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MapPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Configured map prefab: {MapPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static RectTransform EnsureScrollView(Transform canvasTransform, RectTransform nodeContent)
        {
            RectTransform existing = FindDirectChildRectTransform(canvasTransform, "MapScrollView");
            if (existing != null)
            {
                return existing;
            }

            int siblingIndex = nodeContent.GetSiblingIndex();
            GameObject scrollViewObject = new("MapScrollView", typeof(RectTransform));
            RectTransform scrollView = scrollViewObject.GetComponent<RectTransform>();
            scrollView.SetParent(canvasTransform, false);
            scrollView.SetSiblingIndex(siblingIndex);
            return scrollView;
        }

        private static void ConfigureViewport(RectTransform scrollView)
        {
            scrollView.anchorMin = new Vector2(0.5f, 0.5f);
            scrollView.anchorMax = new Vector2(0.5f, 0.5f);
            scrollView.pivot = new Vector2(0.5f, 0.5f);
            scrollView.anchoredPosition = ViewportPosition;
            scrollView.sizeDelta = ViewportSize;

            Image image = scrollView.GetComponent<Image>() ?? scrollView.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            if (scrollView.GetComponent<RectMask2D>() == null)
            {
                scrollView.gameObject.AddComponent<RectMask2D>();
            }
        }

        private static void ConfigureNodeContent(RectTransform scrollView, RectTransform nodeContent)
        {
            nodeContent.SetParent(scrollView, false);
            nodeContent.anchorMin = new Vector2(0.5f, 0.5f);
            nodeContent.anchorMax = new Vector2(0.5f, 0.5f);
            nodeContent.pivot = new Vector2(0.5f, 0.5f);
            nodeContent.anchoredPosition = Vector2.zero;
            nodeContent.sizeDelta = NodeContentSize;
        }

        private static ScrollRect ConfigureScrollRect(RectTransform scrollView, RectTransform nodeContent)
        {
            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>() ?? scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.content = nodeContent;
            scrollRect.viewport = scrollView;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;
            return scrollRect;
        }

        private static MapNodeView ConfigureNodeTemplate(RectTransform nodeContent)
        {
            MapNodeView[] nodeViews = nodeContent.GetComponentsInChildren<MapNodeView>(includeInactive: true);
            MapNodeView template = nodeViews.Length > 0 ? nodeViews[0] : CreateNodeTemplate(nodeContent);
            if (template == null)
            {
                return null;
            }

            template.name = "NodeView_Template";
            template.gameObject.SetActive(false);

            RectTransform templateRect = template.transform as RectTransform;
            if (templateRect != null)
            {
                templateRect.SetParent(nodeContent, false);
                templateRect.anchorMin = new Vector2(0.5f, 0.5f);
                templateRect.anchorMax = new Vector2(0.5f, 0.5f);
                templateRect.pivot = new Vector2(0.5f, 0.5f);
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = CurrentMarkerSize;
            }

            for (int i = 1; i < nodeViews.Length; i++)
            {
                Object.DestroyImmediate(nodeViews[i].gameObject);
            }

            return template;
        }

        private static MapNodeView CreateNodeTemplate(RectTransform nodeContent)
        {
            GameObject nodeViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodeViewPrefabPath);
            if (nodeViewPrefab == null)
            {
                Debug.LogError($"NodeView prefab not found: {NodeViewPrefabPath}");
                return null;
            }

            GameObject templateObject = (GameObject)PrefabUtility.InstantiatePrefab(nodeViewPrefab, nodeContent);
            return templateObject.GetComponent<MapNodeView>();
        }

        private static HpView ConfigurePlayerHpView(Transform canvasTransform)
        {
            HpView existing = FindHpView(canvasTransform);
            if (existing == null)
            {
                GameObject playerHpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHpPrefabPath);
                if (playerHpPrefab == null)
                {
                    Debug.LogError($"Player HP prefab not found: {PlayerHpPrefabPath}");
                    return null;
                }

                GameObject playerHpObject = (GameObject)PrefabUtility.InstantiatePrefab(playerHpPrefab, canvasTransform);
                existing = playerHpObject.GetComponent<HpView>();
            }

            if (existing == null)
            {
                return null;
            }

            existing.name = "PlayerHpSlider";

            if (existing.transform is RectTransform hpRect)
            {
                hpRect.SetParent(canvasTransform, false);
                hpRect.anchorMin = new Vector2(0f, 1f);
                hpRect.anchorMax = new Vector2(0f, 1f);
                hpRect.pivot = new Vector2(0f, 1f);
                hpRect.anchoredPosition = new Vector2(48f, -48f);
                hpRect.sizeDelta = new Vector2(360f, 56f);
            }

            return existing;
        }

        private static HpView FindHpView(Transform parent)
        {
            foreach (HpView hpView in parent.GetComponentsInChildren<HpView>(includeInactive: true))
            {
                if (hpView.name == "PlayerHpSlider")
                {
                    return hpView;
                }
            }

            return null;
        }

        private static void ConfigureController(MapController controller, RectTransform nodeContent, ScrollRect scrollRect, MapNodeView nodeTemplate, HpView playerHpView)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("nodeViewPrefab").objectReferenceValue = nodeTemplate;
            serializedController.FindProperty("nodeContainer").objectReferenceValue = nodeContent;
            serializedController.FindProperty("nodeViews").ClearArray();
            serializedController.FindProperty("mapScrollRect").objectReferenceValue = scrollRect;
            serializedController.FindProperty("createScrollRectIfMissing").boolValue = false;
            serializedController.FindProperty("mapViewportSize").vector2Value = ViewportSize;
            serializedController.FindProperty("mapViewportPosition").vector2Value = ViewportPosition;
            serializedController.FindProperty("centerCurrentNodeOnRefresh").boolValue = true;
            serializedController.FindProperty("animateCurrentNodeScroll").boolValue = true;
            serializedController.FindProperty("currentNodeScrollDuration").floatValue = 0.35f;
            serializedController.FindProperty("playerHpView").objectReferenceValue = playerHpView;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCurrentMarkers(GameObject prefabRoot)
        {
            foreach (MapNodeView nodeView in prefabRoot.GetComponentsInChildren<MapNodeView>(includeInactive: true))
            {
                Transform marker = nodeView.transform.Find("CurrentMarker");
                if (marker is not RectTransform markerRect)
                {
                    continue;
                }

                markerRect.SetAsLastSibling();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0f);
                markerRect.anchoredPosition = CurrentMarkerOffset;
                markerRect.sizeDelta = CurrentMarkerSize;
                markerRect.localScale = Vector3.one;
            }
        }

        private static RectTransform FindRectTransform(GameObject root, string objectName)
        {
            foreach (RectTransform rectTransform in root.GetComponentsInChildren<RectTransform>(includeInactive: true))
            {
                if (rectTransform.name == objectName)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private static RectTransform FindDirectChildRectTransform(Transform parent, string objectName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == objectName && child is RectTransform rectTransform)
                {
                    return rectTransform;
                }
            }

            return null;
        }
    }
}
