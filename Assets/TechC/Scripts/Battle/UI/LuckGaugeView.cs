using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 運ゲージの表示を管理する
    /// </summary>
    public class LuckGaugeView : MonoBehaviour
    {
        [SerializeField] private Slider gaugeSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI gaugeText;
        // [SerializeField] private GameObject hotModeEffect; // 激アツ演出オブジェクト

        [Header("Color")]
        [SerializeField] private Color normalColor = Color.yellow;
        [SerializeField] private Color hotModeColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.3f;

        public void Setup(float max)
        {
            gaugeSlider.maxValue = max;
            gaugeSlider.value = 0f;
            fillImage.color = normalColor;
            UpdateText(0f, max);

            // if (hotModeEffect != null)
            //     hotModeEffect.SetActive(false);
        }

        /// <summary>
        /// ゲージをアニメーション付きで更新する
        /// </summary>
        public async UniTask UpdateGaugeAsync(float current, float max, bool isHotMode)
        {
            await SlideToAsync(current, max);
            UpdateHotMode(isHotMode);
        }

        /// <summary>
        /// アニメーションなしで即時反映（運ゲージ消費時など）
        /// </summary>
        public void UpdateGaugeImmediate(float current, float max, bool isHotMode)
        {
            gaugeSlider.value = current;
            UpdateText(current, max);
            UpdateHotMode(isHotMode);
        }

        private async UniTask SlideToAsync(float current, float max)
        {
            float from = gaugeSlider.value;
            float t = 0;

            while (t < slideDuration)
            {
                t += Time.deltaTime;
                gaugeSlider.value = Mathf.Lerp(from, current, t / slideDuration);
                UpdateText(gaugeSlider.value, max);
                await UniTask.Yield();
            }

            gaugeSlider.value = current;
            UpdateText(current, max);
        }

        private void UpdateHotMode(bool isHotMode)
        {
            fillImage.color = isHotMode ? hotModeColor : normalColor;

            // if (hotModeEffect != null)
            //     hotModeEffect.SetActive(isHotMode);
        }

        private void UpdateText(float current, float max)
        {
            if (gaugeText != null)
                gaugeText.text = $"{(int)current}/{(int)max}";
        }
    }
}