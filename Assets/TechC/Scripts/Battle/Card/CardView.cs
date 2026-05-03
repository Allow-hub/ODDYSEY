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
    ///   - Update() での毎フレーム比較を廃止。
    ///     CardInstance.OnSlotValueChanged イベントを購読し、
    ///     値が変わったときだけ RefreshDisplay() を呼ぶようにした。
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

        // ▼ 削除：lastProbability / lastDamage フィールドは不要になった
        // private float lastProbability;
        // private int lastDamage;

        private bool isPlaced = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(CardInstance cardInstance)
        {
            // ▼ 前のインスタンスの購読を必ず解除してからセットアップ
            UnsubscribeFromInstance();

            this.cardInstance = cardInstance;
            cardData = cardInstance.OriginalData;
            instanceId = cardInstance.InstanceId;
            isPlaced = false;

            rootCanvas = GetComponentInParent<Canvas>();

            // ▼ 値変更イベントを購読
            cardInstance.OnSlotValueChanged += RefreshDisplay;

            RefreshDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInstance();
        }

        /// <summary>
        /// 購読を安全に解除する。Setup 前・OnDestroy 両方から呼ぶ。
        /// </summary>
        private void UnsubscribeFromInstance()
        {
            if (cardInstance != null)
                cardInstance.OnSlotValueChanged -= RefreshDisplay;
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

        public async UniTask PlayBreakAnimationAsync()
        {
            await UniTask.Delay(1);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (isDealing || isEnemy) return;

            if (isPlaced)
            {
                CustomLogger.Info($"配置済みカードクリック: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
                BattleEventBus.Publish(new CardPlacedClickedEvent(this));
            }
            else
            {
                CardDetailView.I.Show(cardData);
                CustomLogger.Info($"カード情報を表示: {cardData.CardName} (InstanceId: {instanceId})", LogTagUtil.TagCard);
            }
        }

        public void SetPlacedParent(Transform newParent)
        {
            isPlaced = true;
            transform.SetParent(newParent);
            transform.localPosition = Vector3.zero;
            cardImage.raycastTarget = true;
        }

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