using System;
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
    /// カードは消滅し、LuckConversionRate の分だけ運ゲージが回復する。
    /// </summary>
    public class BreakZoneView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("見た目")]
        [SerializeField] private Image highlightImage;

        [Header("ハイライトカラー")]
        [SerializeField] private Color highlightColor = new Color(0.9f, 0.5f, 0.1f, 0.8f);

        [SerializeField] private BattleView battleView;

        /// <summary>
        /// カードが砕かれたときに呼ばれるコールバック。
        /// 引数は（砕かれた CardView, 回復する運ゲージ量）。
        /// BattleController / PlayZonePresenter が登録する。
        /// </summary>
        public event Action<CardView, float> OnCardBroken;

        // -------------------------------------------------------
        // IDropHandler
        // -------------------------------------------------------

        public void OnDrop(PointerEventData eventData)
        {
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null) return;

            // スロットに配置済みのカードは砕けない
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

            CustomLogger.Info($"カードを砕く: {cardView.CardData.CardName} → 運ゲージ +{luckGain}",LogTagUtil.TagCard);

            // 演出を再生してから通知（演出は CardView 側が持つ）
            PlayBreakAndNotify(cardView, luckGain).Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid PlayBreakAndNotify(CardView cardView, float luckGain)
        {
            await cardView.PlayBreakAnimationAsync();
            battleView.RemoveCardAsync(cardView.InstanceId).Forget(); // アニメーション完了後にカードを View から削除

            // アニメーション完了後にロジック側へ通知
            // （PlayBreakAnimationAsync の中で Destroy まで行う）
            OnCardBroken?.Invoke(cardView, luckGain);
        }

        private void SetHighlight(bool on)
        {
            if (highlightImage != null)
                highlightImage.color = on ? highlightColor : Color.white;
        }
    }
}