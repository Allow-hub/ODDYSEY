using Cysharp.Threading.Tasks;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    public class BreakZoneView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("見た目")]
        [SerializeField] private Image highlightImage; // 加算合成用の専用 Image

        [Header("ハイライト設定")]
        [SerializeField] private Material electricMaterial;
        [SerializeField] private float    fadeSpeed = 6f;

        [SerializeField] private BattleView battleView;

        private Material instancedMaterial;
        private float    targetIntensity  = 0f;
        private float    currentIntensity = 0f;

        private static readonly int IntensityPropId = Shader.PropertyToID("_Intensity");
        private static readonly int LocalTimePropId = Shader.PropertyToID("_LocalTime");

        private void Awake()
        {
            if (electricMaterial == null || highlightImage == null) return;

            instancedMaterial       = new Material(electricMaterial);
            highlightImage.material = instancedMaterial;
            instancedMaterial.SetFloat(IntensityPropId, 0f);
            instancedMaterial.SetFloat(LocalTimePropId, 0f);
        }

        private void OnDestroy()
        {
            if (instancedMaterial != null)
                Destroy(instancedMaterial);
        }

        private void Update()
        {
            if (instancedMaterial == null) return;

            // Intensity をフェード
            currentIntensity = Mathf.MoveTowards(
                currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);

            instancedMaterial.SetFloat(IntensityPropId, currentIntensity);
            instancedMaterial.SetFloat(LocalTimePropId, Time.time);
        }

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

        private void SetHighlight(bool on)
        {
            targetIntensity = on ? 1f : 0f;
        }

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
    }
}