using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 運ゲージの表示を管理する。
    ///
    /// 変更点：
    ///   - LuckGaugeChangedEvent を購読。
    ///     PlayZoneView のカード強化でゲージが消費されたとき、
    ///     アニメーションなしで即時 UI を更新する。
    ///   - ターン終了時の UpdateGaugeAsync はそのまま（アニメあり）。
    /// </summary>
    public class LuckGaugeView : MonoBehaviour
    {
        [SerializeField] private Slider gaugeSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI gaugeText;

        [Header("Color")]
        [SerializeField] private Color normalColor = Color.yellow;
        [SerializeField] private Color hotModeColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.3f;

        private void OnEnable()
        {
            // ゲージ即時変化（カード強化によるゲージ消費など）を購読
            BattleEventBus.Subscribe<LuckGaugeChangedEvent>(OnLuckGaugeChanged);
        }

        private void OnDisable()
        {
            BattleEventBus.Unsubscribe<LuckGaugeChangedEvent>(OnLuckGaugeChanged);
        }

        public void Setup(float max)
        {
            gaugeSlider.maxValue = max;
            gaugeSlider.value = 0f;
            fillImage.color = normalColor;
            UpdateText(0f, max);
        }

        /// <summary>
        /// ゲージをアニメーション付きで更新する（ターン終了時など）。
        /// </summary>
        public async UniTask UpdateGaugeAsync(float current, float max, bool isHotMode)
        {
            await SlideToAsync(current, max);
            UpdateHotMode(isHotMode);
        }

        /// <summary>
        /// アニメーションなしで即時反映（Setup 時・運ゲージ消費時）。
        /// </summary>
        public void UpdateGaugeImmediate(float current, float max, bool isHotMode)
        {
            gaugeSlider.value = current;
            UpdateText(current, max);
            UpdateHotMode(isHotMode);
        }

        // ──────────────────────────────────────────────────────────
        // イベント受信
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// BattleController がゲージ変化後に発行するイベントを受信して即時更新。
        /// アニメーションは不要（プレイヤー操作への即時フィードバック）。
        /// </summary>
        private void OnLuckGaugeChanged(LuckGaugeChangedEvent ev)
        {
            UpdateGaugeImmediate(ev.Current, ev.Max, ev.IsHotMode);
        }

        // ──────────────────────────────────────────────────────────
        // 内部処理
        // ──────────────────────────────────────────────────────────

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
        }

        private void UpdateText(float current, float max)
        {
            if (gaugeText != null)
                gaugeText.text = $"{(int)current}/{(int)max}";
        }
    }
}