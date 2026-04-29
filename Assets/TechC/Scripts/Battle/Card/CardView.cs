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
    ///
    /// 変更点：
    ///   - onReturnRequested / onDroppedToSlot の Action を廃止。
    ///     代わりに BattleEventBus にイベントを発行する。
    ///   - isPlaced=true のときのクリックで ReturnToHand を直接呼ぶのをやめ、
    ///     CardPlacedClickedEvent を発行するだけにした。
    ///     「戻すかどうか」の判断は PlayZonePresenter 側に委譲。
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
        private CardInstance cardInstance;
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

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Setup から Action の引数を削除。
        /// 購読は呼び出し側（BattleView / PlayZonePresenter）が BattleEventBus で行う。
        /// </summary>
        public void Setup(CardInstance cardInstance)
        {
            this.cardInstance = cardInstance;
            cardData = cardInstance.OriginalData;
            instanceId = cardInstance.InstanceId;
            isPlaced = false;

            rootCanvas = GetComponentInParent<Canvas>();
            RefreshDisplay();
        }

        public void SetEnemyAppearance()
        {
            isEnemy = true;
        }

        private void RefreshDisplay()
        {
            if (cardData == null) return;
            cardNameText.text = cardData.CardName;
            var probability = cardInstance.GetEffectiveProbability(0) * 100;
            probabilityText.text = $"{probability:F0}%";
            damageText.text = $"{cardInstance.GetEffectiveValue(0)}";
        }

        /// <summary>
        /// ドロー・手札追加時のアニメーション。
        /// </summary>
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
        /// カードを砕くアニメーション。
        /// </summary>
        public async UniTask PlayBreakAnimationAsync()
        {
            await UniTask.Delay(1); // 仮
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDealing || isPlaced || isEnemy) return;
            cardImage.raycastTarget = false;
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
        /// ドラッグ終了。
        /// スロット未配置なら手札位置へ戻し、CardReturnedToHandEvent を発行。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDealing || isEnemy) return;
            if (isPlaced)
            {
                CustomLogger.Info($"カード配置確定: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
                return;
            }

            cardImage.raycastTarget = true;
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = dealTargetPos;

            CustomLogger.Info($"カード配置キャンセル: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
            BattleEventBus.Publish(new CardReturnedToHandEvent(this));
        }

        /// <summary>
        /// クリック処理。
        ///   isPlaced=true → CardPlacedClickedEvent を発行。ReturnToHand は呼ばない。
        ///   isPlaced=false → カード詳細を表示。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (isDealing || isEnemy) return;

            if (isPlaced)
            {
                // 「戻すかどうか」は PlayZonePresenter が判断する
                CustomLogger.Info($"配置済みカードクリック: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
                BattleEventBus.Publish(new CardPlacedClickedEvent(this));
            }
            else
            {
                CardDetailView.I.Show(cardData);
                CustomLogger.Info($"カード情報を表示: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
            }
        }

        /// <summary>
        /// スロットへの配置が確定したときに PlayZoneSlotView から呼ぶ。
        /// </summary>
        public void SetPlacedParent(Transform newParent)
        {
            isPlaced = true;
            transform.SetParent(newParent);
            transform.localPosition = Vector3.zero;
            cardImage.raycastTarget = true;
        }

        /// <summary>
        /// スロットから手札へ戻す。PlayZonePresenter から呼ぶ。
        /// </summary>
        public void ReturnToHand(Transform handParent)
        {
            isPlaced = false;
            transform.SetParent(handParent);
            rectTransform.anchoredPosition = dealTargetPos;
        }

        public CardInstance CardInstance => cardInstance;
        public CardData CardData => cardData;
        public int InstanceId => instanceId;
        public bool IsPlaced => isPlaced;
    }
}