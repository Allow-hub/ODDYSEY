using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.Core.Manager
{
    public class FadeManager : Singleton<FadeManager>
    {
        [SerializeField] private float fadeDuration = 0.5f;

        private CanvasGroup canvasGroup;

        protected override bool DontDestroy => true;

        protected override void OnInit()
        {
            base.OnInit();
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGO.AddComponent<CanvasScaler>();

            var panelGO = new GameObject("FadePanel");
            panelGO.transform.SetParent(canvasGO.transform, false);

            var image = panelGO.AddComponent<Image>();
            image.color = Color.black;

            var rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            canvasGroup = panelGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public async UniTask FadeOutAsync()
        {
            canvasGroup.blocksRaycasts = true;
            await TweenAlphaAsync(0f, 1f);
        }

        public async UniTask FadeInAsync()
        {
            await TweenAlphaAsync(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        private async UniTask TweenAlphaAsync(float from, float to)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            canvasGroup.alpha = to;
        }
    }
}
