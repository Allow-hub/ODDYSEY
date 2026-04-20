using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// マップ上の選択肢ボタン1つ。
    /// NodeType に応じたラベル表示とコールバック通知を担当する。
    /// </summary>
    public class NodeChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        // [SerializeField] private TextMeshProUGUI label;
        // [SerializeField] private Image iconImage;

        // NodeType ごとのアイコンは Inspector で差し替え可能にする
        [Header("アイコン設定")]
        [SerializeField] private Sprite battleIcon;
        [SerializeField] private Sprite eventIcon;
        [SerializeField] private Sprite restIcon;

        private Action<NodeType> onSelected;

        /// <summary>
        /// ボタンをセットアップする。
        /// </summary>
        /// <param name="nodeType">この選択肢の種類</param>
        /// <param name="callback">選択時に呼ぶコールバック</param>
        public void Setup(NodeType nodeType, Action<NodeType> callback)
        {
            onSelected = callback;

            // label.text = GetLabel(nodeType);
            // if (iconImage != null)
            //     iconImage.sprite = GetIcon(nodeType);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(nodeType));
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        private string GetLabel(NodeType type) => type switch
        {
            NodeType.Battle => "⚔ 戦闘",
            NodeType.Event  => "❓ イベント",
            NodeType.Rest   => "🏕 休憩",
            _               => type.ToString(),
        };

        private Sprite GetIcon(NodeType type) => type switch
        {
            NodeType.Battle => battleIcon,
            NodeType.Event  => eventIcon,
            NodeType.Rest   => restIcon,
            _               => null,
        };
    }
}