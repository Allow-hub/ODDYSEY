using System;
using TMPro;
using TechC.ODDESEY.Event;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TechC.ODDESEY.Map
{
    public class NodeChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [FormerlySerializedAs("tileImage")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI label;

        [Header("Labels")]
        [SerializeField] private string battleLabel = "戦闘";
        [SerializeField] private string healLabel = "回復";
        [SerializeField] private string cardLabel = "カード";
        [SerializeField] private string riskLabel = "危険";
        [SerializeField] private string restLabel = "休憩";

        private Action onSelected;

        public void Setup(NodeType nodeType, Action<NodeType> callback)
        {
            Setup(nodeType, EventMapIconType.Risk, () => callback?.Invoke(nodeType));
        }

        public void Setup(NodeType nodeType, EventMapIconType eventIconType, Action callback)
        {
            onSelected = callback;

            if (backgroundImage != null)
            {
                backgroundImage.enabled = true;
            }

            if (label != null)
            {
                label.text = GetLabel(nodeType, eventIconType);
                label.gameObject.SetActive(true);
            }

            if (button == null)
            {
                Debug.LogError($"[NodeChoiceButton] {gameObject.name}: Button is not assigned.");
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke());
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private string GetLabel(NodeType type, EventMapIconType eventIconType) => type switch
        {
            NodeType.Battle => battleLabel,
            NodeType.Event => eventIconType switch
            {
                EventMapIconType.Card => cardLabel,
                EventMapIconType.Heal => healLabel,
                EventMapIconType.Risk => riskLabel,
                _ => riskLabel,
            },
            NodeType.Rest => restLabel,
            _ => type.ToString(),
        };
    }
}
