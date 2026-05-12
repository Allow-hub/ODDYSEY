using System;
using System.Collections.Generic;
using TechC.ODDESEY.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// カード報酬選択画面の表示を管理する View クラス。
    ///
    /// Prefab 構成：
    ///   CardRewardView（GameObject）
    ///     ├─ titleText（TextMeshProUGUI）
    ///     ├─ cardContainer（Transform）  ← CardRewardSlot を動的に生成
    ///     └─ skipButton（Button）
    /// </summary>
    public class CardRewardView : MonoBehaviour
    {
        // [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardSlotPrefab; // CardRewardSlot をアタッチした Prefab
        [SerializeField] private Button skipButton;

        private readonly List<GameObject> spawnedSlots = new();

        public void Setup(
            List<CardData> offers,
            Action<CardData> onSelected,
            Action onSkipped)
        {
            // if (titleText != null)
            //     titleText.text = "カードを1枚選んでください";

            // 既存スロットをクリア
            foreach (var slot in spawnedSlots)
                Destroy(slot);
            spawnedSlots.Clear();

            // カードスロットを生成
            foreach (var card in offers)
            {
                var obj = Instantiate(cardSlotPrefab, cardContainer);
                var slot = obj.GetComponent<CardRewardSlot>();
                slot.Setup(card, onSelected);
                spawnedSlots.Add(obj);
            }

            // スキップボタン
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() => onSkipped?.Invoke());
            }

            gameObject.SetActive(true);
        }
    }
}