using System;
using TechC.ODDESEY.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// 報酬選択画面のカード1枚分のスロット。
    /// CardRewardView から動的に生成される。
    /// </summary>
    public class CardRewardSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private Image cardImage;
        [SerializeField] private Button selectButton;

        public void Setup(CardData card, Action<CardData> onSelected)
        {
            if (cardNameText != null) cardNameText.text = card.CardName;
            if (cardImage != null) cardImage.sprite = card.CardSprite;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(card));
        }
    }
}