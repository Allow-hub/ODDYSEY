using System;
using TechC.ODDESEY.Event;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// Visual-only node view for the map graph.
    /// Interaction is intentionally handled by NodeChoiceButton, not by this object.
    /// </summary>
    public class MapNodeView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Image tileImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject currentMarker;
        [SerializeField] private bool alignCurrentMarkerBottomToNodeCenter = true;
        [SerializeField] private Vector2 currentMarkerSize = new(100f, 100f);
        [SerializeField] private Vector2 currentMarkerOffset = new(0f, -12f);

        [Header("Tile Sprites")]
        [SerializeField] private Sprite tileNoneSprite;
        [SerializeField] private Sprite tileOffSprite;
        [SerializeField] private Sprite tileBattleSprite;
        [SerializeField] private Sprite tileHealSprite;
        [SerializeField] private Sprite tileCardSprite;
        [SerializeField] private Sprite tileRiskSprite;

        [Header("Icon Sprites")]
        [SerializeField] private Sprite iconBattleSprite;
        [SerializeField] private Sprite iconHealSprite;
        [SerializeField] private Sprite iconCardSprite;
        [SerializeField] private Sprite iconRiskSprite;

        public enum NodeState { Locked, Active, Cleared, Current }

        private void Awake()
        {
            ApplyCurrentMarkerLayout();
        }

        private void OnValidate()
        {
            ApplyCurrentMarkerLayout();
        }

        public void Setup(StageNodeData data, NodeState state)
        {
            if (data == null)
            {
                Debug.LogError($"[MapNodeView] {gameObject.name}: StageNodeData is null.");
                return;
            }

            EventMapIconType eventIconType = GetEventIconType(data);
            ApplyVisualState(data, state, eventIconType);
        }

        private void ApplyVisualState(StageNodeData data, NodeState state, EventMapIconType eventIconType)
        {
            NodeType displayType = data.nodeType;

            if (tileImage != null)
            {
                tileImage.sprite = state switch
                {
                    NodeState.Cleared => tileOffSprite,
                    NodeState.Current => tileOffSprite,
                    NodeState.Active => GetActiveTileSprite(displayType, eventIconType),
                    _ => tileNoneSprite,
                };
                tileImage.enabled = tileImage.sprite != null;
            }

            if (iconImage != null)
            {
                iconImage.sprite = GetIconSprite(displayType, eventIconType);
                iconImage.enabled = (state == NodeState.Active || state == NodeState.Locked) && iconImage.sprite != null;
            }

            if (currentMarker != null)
            {
                ApplyCurrentMarkerLayout();
                currentMarker.SetActive(state == NodeState.Current);
            }
        }

        private void ApplyCurrentMarkerLayout()
        {
            if (currentMarker == null)
            {
                return;
            }

            // The player marker is a location marker, not a node decoration.
            // Its bottom edge marks the node center, so the standing art sits on the node.
            currentMarker.transform.SetAsLastSibling();

            if (!alignCurrentMarkerBottomToNodeCenter)
            {
                return;
            }

            if (currentMarker.transform is not RectTransform markerRect)
            {
                return;
            }

            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0f);
            markerRect.anchoredPosition = currentMarkerOffset;
            markerRect.sizeDelta = currentMarkerSize;
            markerRect.localScale = Vector3.one;
        }

        private static EventMapIconType GetEventIconType(StageNodeData data)
        {
            return data.EventData != null
                ? data.EventData.MapIconType
                : EventMapIconType.Risk;
        }

        private Sprite GetActiveTileSprite(NodeType nodeType, EventMapIconType eventIconType)
        {
            return nodeType switch
            {
                NodeType.Battle => tileBattleSprite,
                NodeType.Rest => tileHealSprite,
                NodeType.Event => eventIconType switch
                {
                    EventMapIconType.Card => tileCardSprite,
                    EventMapIconType.Heal => tileHealSprite,
                    EventMapIconType.Risk => tileRiskSprite,
                    _ => tileRiskSprite,
                },
                _ => tileNoneSprite,
            };
        }

        private Sprite GetIconSprite(NodeType nodeType, EventMapIconType eventIconType)
        {
            return nodeType switch
            {
                NodeType.Battle => iconBattleSprite,
                NodeType.Rest => iconHealSprite,
                NodeType.Event => eventIconType switch
                {
                    EventMapIconType.Card => iconCardSprite,
                    EventMapIconType.Heal => iconHealSprite,
                    EventMapIconType.Risk => iconRiskSprite,
                    _ => iconRiskSprite,
                },
                _ => null,
            };
        }
    }
}
