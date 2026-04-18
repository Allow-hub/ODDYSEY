using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using TechC.VBattle.Core.Extensions;
using TechC.ODDESEY.Util;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 手札のカード1枚を表す View クラス。
    /// </summary>
    public class CardView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("テキスト")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI probabilityText;
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("見た目")]
        [SerializeField] private Image cardImage;

        [Header("アニメ設定")]
        [SerializeField] private float moveDuration = 0.25f;

        private Animator animator;
        private RectTransform rectTransform;
        private CardData cardData;
        private int instanceId;
        private bool isEnemy = false;



        public Transform OriginalParent => originalParent;
        private Transform originalParent;
        private Canvas rootCanvas;
        private Vector2 dealTargetPos;
        private bool isDealing = false;

        /// <summary>
        /// スロットへの配置が確定したかどうか。
        /// EventSystem は OnDrop → OnEndDrag の順で呼ぶため、
        /// OnDrop 側で true にしておくと OnEndDrag の手札戻しを防げる。
        /// </summary>
        private bool isPlaced = false;

        private Action<CardView> onDroppedToSlot;
        private Action<CardView> onReturnRequested;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(
            CardData cardData,
            int instanceId = 0,
            Action<CardView> onReturnRequested = null,
            Action<CardView> onDroppedToSlot = null)
        {
            this.cardData = cardData;
            this.instanceId = instanceId;
            this.onDroppedToSlot = onDroppedToSlot;
            this.onReturnRequested = onReturnRequested;
            isPlaced = false;

            rootCanvas = GetComponentInParent<Canvas>();
        }


        public void SetEnemyAppearance()
        {
            isEnemy = true;
        }


        private void RefreshDisplay()
        {
            if (cardData == null) return;
            cardNameText.text = cardData.CardName;
        }

        /// <summary>
        /// ドロー、手札への追加時のアニメーション。カードが山札から手札に移動する演出
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        public async UniTask PlayDealAnimationAsync(Vector2 startPos, Vector2 targetPos)
        {
            isDealing = true;
            dealTargetPos = targetPos;

            float elapsed = 0f;
            rectTransform.anchoredPosition = startPos;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                await UniTask.Yield();
            }

            rectTransform.anchoredPosition = targetPos;
            isDealing = false;
        }

        /// <summary>
        /// カードを砕くアニメーション。砕ける演出を再生してから完了通知を送る
        /// </summary>
        /// <returns></returns>
        public async UniTask PlayBreakAnimationAsync()
        {
            // Destroy(gameObject);
            await UniTask.Delay(1); // 仮
        }

        // public void OnBreakAnimationComplete()
        // {
        //     breakTcs?.TrySetResult();
        //     Destroy(gameObject);
        // }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDealing || isPlaced || isEnemy) return;
            cardImage.raycastTarget = false; // ドロップ判定の邪魔になるのでドラッグ中は無効化
            originalParent = transform.parent;
            transform.SetParent(rootCanvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDealing || isPlaced || isEnemy) return;

            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var worldPos
            );
            transform.position = worldPos;
        }

        /// <summary>
        /// ドラッグ終了。isPlaced = true なら配置確定済みなので何もしない。
        /// false なら手札位置へ戻す。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDealing || isEnemy) return;
            if (isPlaced)
            {
                // スロット配置確定済み → 何もしない（OnDrop 側で処理済み）
                CustomLogger.Info($"カード配置確定: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
                return;
            }
            cardImage.raycastTarget = true; // ドロップ判定を再度有効化

            // スロットに入らなかった → 手札位置へ戻す
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = dealTargetPos;
            CustomLogger.Info($"カード配置キャンセル: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
            onReturnRequested?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (isDealing || isEnemy) return;
            if (isPlaced)
            {
                ReturnToHand(originalParent);
                CustomLogger.Info($"カードを手札に戻す: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
                onReturnRequested?.Invoke(this);
            }
            else
            {
                CardDetailView.I.Show(cardData);
                CustomLogger.Info($"TODO:カード情報を表示: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
            }
        }

        /// <summary>
        /// スロットへの配置が確定したときに PlayZoneSlotView から呼ぶ。
        /// </summary>
        public void SetPlacedParent(Transform newParent)
        {
            isPlaced = true;//OnEndDrag の手札戻しをブロックする
            transform.SetParent(newParent); // anchoredPosition リセットもここで行う
            transform.localPosition = Vector3.zero;
            cardImage.raycastTarget = true; // ドロップ判定を再度有効化
        }

        /// <summary>
        /// スロットから手札へ戻すときに呼ぶ（スロットの取り外しボタンなど）。
        /// </summary>
        public void ReturnToHand(Transform handParent)
        {
            isPlaced = false;
            transform.SetParent(handParent);
            rectTransform.anchoredPosition = dealTargetPos;
        }

        public CardData CardData => cardData;
        public int InstanceId => instanceId;
        public Vector2 DealTargetPos => dealTargetPos;
        public bool IsDealing => isDealing;
        public bool IsPlaced => isPlaced;
    }
}