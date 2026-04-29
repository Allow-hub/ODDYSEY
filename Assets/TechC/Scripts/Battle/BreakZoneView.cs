using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カードをドロップすると「砕く」処理を行う専用UIエリア。
    ///
    /// 変更点：
    ///   - OnCardBroken event を廃止。
    ///     BattleController が直接購読していた event の代わりに
    ///     BattleEventBus.Publish(CardBrokenEvent) で通知する。
    ///   - BattleController 側の OnCardBroken -= 購読解除も不要になった。
    /// </summary>
    public class BreakZoneView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("見た目")]
        [SerializeField] private Image highlightImage;

        [Header("ハイライトカラー")]
        [SerializeField] private Color highlightColor = new Color(0.9f, 0.5f, 0.1f, 0.8f);

        [SerializeField] private BattleView battleView;

        // -------------------------------------------------------
        // IDropHandler
        // -------------------------------------------------------

        public void OnDrop(PointerEventData eventData)
        {
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null) return;

            if (cardView.IsPlaced)
            {
                CustomLogger.Info($"配置済みのカードは砕けない: {cardView.CardData.CardName}", LogTagUtil.TagCard);
                return;
            }

            SetHighlight(false);
            BreakCard(cardView);
        }

        // -------------------------------------------------------
        // IPointerEnterHandler / IPointerExitHandler
        // -------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView == null || cardView.IsPlaced) return;

            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        // -------------------------------------------------------
        // 砕く処理
        // -------------------------------------------------------

        private void BreakCard(CardView cardView)
        {
            float luckGain = cardView.CardData.LuckConversionRate;
            CustomLogger.Info($"カードを砕く: {cardView.CardData.CardName} → 運ゲージ +{luckGain}", LogTagUtil.TagCard);
            PlayBreakAndNotify(cardView, luckGain).Forget();
        }

        private async UniTaskVoid PlayBreakAndNotify(CardView cardView, float luckGain)
        {
            await cardView.PlayBreakAnimationAsync();
            battleView.RemoveCardAsync(cardView.InstanceId).Forget();

            BattleEventBus.Publish(new CardBrokenEvent(cardView, luckGain));
        }

        private void SetHighlight(bool on)
        {
            if (highlightImage != null)
                highlightImage.color = on ? highlightColor : Color.white;
        }
    }
}