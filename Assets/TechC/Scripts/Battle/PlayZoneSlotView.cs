using System;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// プレイゾーンのスロット1つを表す View クラス。
    /// </summary>
    public class PlayZoneSlotView : MonoBehaviour,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("参照")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image highlightImage;  // backgroundImage とは別 GameObject にアサイン
        [SerializeField] private Transform cardAnchor;      // 配置カードの親になる Transform

        [Header("背景カラー")]
        [SerializeField] private Color highlightColor = new Color(0.4f, 0.9f, 0.4f, 0.8f);

        private PlayZoneSlot slotData;
        private CardView placedCardView;
        private int slotIndex;

        private Action<int, CardView> onCardPlaced;
        private Action<int> onCardRemoved;


        public void Setup(
            int slotIndex,
            Action<int, CardView> onCardPlaced,
            Action<int> onCardRemoved)
        {
            this.slotIndex = slotIndex;
            this.onCardPlaced = onCardPlaced;
            this.onCardRemoved = onCardRemoved;

            placedCardView = null;
            SetHighlight(false);
        }

        public void SetSlotData(PlayZoneSlot slot)
        {
            slotData = slot;

            if (slot != null && slot.IsEnemyCard && slot.EnemyCardInstance != null)
            {
                // cardAnchor 配下に既にある backgroundImage などを使って表示するだけ
                // 例：背景色を変えて「敵カード」と分かるようにする
                CustomLogger.Info($"スロット {slotIndex} に敵カード配置: {slot.EnemyCardInstance.OriginalData.CardName}", LogTagUtil.TagCard);

                // カード名・スプライトも既存の UI に流し込める
                // （CardView を使わず直接 Image/TMP を参照する設計ならそちらへ）
            }
            else
            {
                if (backgroundImage != null)
                    backgroundImage.color = Color.white;
            }
        }

        public void ClearSlot()
        {
            if (placedCardView != null)
            {
                Destroy(placedCardView.gameObject);
                placedCardView = null;
            }
            slotData = null;
            SetHighlight(false);
        }

        // -------------------------------------------------------
        // IDropHandler
        // -------------------------------------------------------

        public void OnDrop(PointerEventData eventData)
        {
            if (!CanAccept()) return;

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null) return;
            SetHighlight(false);
            CustomLogger.Info($"カード配置要求: {cardView.name} → Slot {slotIndex}", LogTagUtil.TagBattle);
            PlaceCard(cardView);
        }

        // -------------------------------------------------------
        // IPointerEnterHandler / IPointerExitHandler
        // -------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            if (eventData.pointerDrag.GetComponent<CardView>() == null) return;
            if (!CanAccept()) return;

            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        // -------------------------------------------------------
        // 配置処理
        // -------------------------------------------------------

        private void PlaceCard(CardView cardView)
        {
            placedCardView = cardView;
            cardView.SetPlacedParent(cardAnchor);
            onCardPlaced?.Invoke(slotIndex, cardView);
        }

        public void RemoveCard(Transform handParent)
        {
            if (placedCardView == null) return;

            placedCardView.ReturnToHand(handParent);
            placedCardView = null;
            SetHighlight(false);
            onCardRemoved?.Invoke(slotIndex);
        }

        private void SetHighlight(bool on)
        {
            if (highlightImage == null) return;
            highlightImage.color = on ? highlightColor : Color.white;
            // CustomLogger.Info($"スロット {slotIndex} ハイライト {(on ? "ON" : "OFF")}", LogTagUtil.TagBattle);
        }

        private bool CanAccept()
        {
            if (slotData != null && slotData.IsEnemyCard) return false;
            return placedCardView == null;
        }
        public CardView PlacedCardView => placedCardView;
    }
}