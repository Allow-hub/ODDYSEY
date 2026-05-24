using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI damageTextShadow;

        [Header("アニメ設定")]
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float floatDuration = 0.4f;
        [SerializeField] private float holdDuration = 0.2f;
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("スケール設定")]
        [SerializeField] private float popScale = 1.4f;
        [SerializeField] private float normalScale = 1.0f;
        [SerializeField] private float popDuration = 0.1f;

        [Header("色設定")]
        [SerializeField] private Color playerDamageColor = new Color(1.0f, 0.3f, 0.3f);
        [SerializeField] private Color enemyDamageColor = new Color(1.0f, 0.85f, 0.1f);
        [SerializeField] private Color criticalColor = new Color(1.0f, 0.5f, 0.0f);
        [SerializeField] private Color missColor = new Color(0.8f, 0.8f, 0.8f); // グレー

        private RectTransform rectTransform;
        private Vector2 startPos;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// ダメージ or Miss を表示する。
        /// damage=0 かつ isHit=false のとき "Miss" を表示する。
        /// </summary>
        public void Show(
            int damage,
            bool isHit,
            bool isPlayerDamage,
            bool isCritical,
            Vector3 worldPos,
            Canvas canvas)
        {
            gameObject.SetActive(true);

            // ワールド座標 → Canvas ローカル座標
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos),
                canvas.worldCamera,
                out var localPos
            );

            localPos += new Vector2(
                Random.Range(-30f, 30f),
                Random.Range(0f, 30f));

            rectTransform.localPosition = localPos;
            startPos = localPos;

            // テキストと色
            bool isMiss = !isHit;
            if (isMiss)
            {
                damageText.text = "Miss";
                damageText.color = missColor;
            }
            else
            {
                string prefix = isCritical ? "‼ " : "";
                damageText.text = $"{prefix}{damage}";
                damageText.color = isCritical ? criticalColor
                                 : isPlayerDamage ? playerDamageColor
                                 : enemyDamageColor;
            }

            if (damageTextShadow != null)
            {
                damageTextShadow.text = damageText.text;
                damageTextShadow.color = new Color(0f, 0f, 0f, 0.5f);
            }

            rectTransform.localScale = Vector3.one * popScale;
            AnimateAsync().Forget();
        }

        private async UniTaskVoid AnimateAsync()
        {
            // ① ポップ（大→通常）
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scale = Mathf.Lerp(popScale, normalScale, t);
                rectTransform.localScale = Vector3.one * scale;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            rectTransform.localScale = Vector3.one * normalScale;

            // ② 上に浮く
            elapsed = 0f;
            while (elapsed < floatDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / floatDuration);
                float ease = 1f - Mathf.Pow(1f - t, 2f);
                rectTransform.localPosition = startPos + Vector2.up * (floatDistance * ease);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // ③ 頂点で待つ
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(holdDuration),
                ignoreTimeScale: true);

            // ④ フェードアウト
            elapsed = 0f;
            Color startColor = damageText.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(1f, 0f, t);
                damageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                if (damageTextShadow != null)
                    damageTextShadow.color = new Color(0f, 0f, 0f, 0.5f * (1f - t));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            gameObject.SetActive(false);
        }
    }
}