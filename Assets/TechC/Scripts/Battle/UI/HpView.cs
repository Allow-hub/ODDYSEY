using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵とプレイヤー共用のHP表示クラス。
    ///
    /// 変更点：
    ///   - SlideToAsync と ResetColorAsync を並列実行に変更。
    ///     HPが減るアニメーションと色リセットが同時に動くのでメリハリが出る。
    ///   - Time.deltaTime → Time.unscaledDeltaTime に変更。
    ///     将来的にヒットストップ（TimeScale 変更）を入れても
    ///     HPバーのアニメーション速度が狂わない。
    ///   - UpdateHpAsync が既に同じ値なら即リターン。
    ///     ミス時など DamageDealt=0 で呼ばれても無駄な待ちが発生しない。
    /// </summary>
    public class HpView : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Color")]
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color damagedColor = Color.red;
        [SerializeField] private float colorResetDuration = 0.4f;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.25f;

        public void Setup(int maxHp)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
            fillImage.color = normalColor;
            UpdateText(maxHp, maxHp);
        }

        /// <summary>
        /// HPバーを更新する。
        /// スライドアニメーションと色リセットを並列で実行するのでテンポよく見える。
        /// </summary>
        public async UniTask UpdateHpAsync(int currentHp, int maxHp)
        {
            // 変化がなければ即リターン
            if (Mathf.Approximately(hpSlider.value, currentHp)) return;

            fillImage.color = damagedColor;

            // スライドと色リセットを並列実行
            await UniTask.WhenAll(
                SlideToAsync(currentHp, maxHp),
                ResetColorAsync()
            );
        }

        /// <summary>
        /// アニメーションなしで即時更新（バトル開始時など）。
        /// </summary>
        public void UpdateImmediate(int currentHp, int maxHp)
        {
            hpSlider.value = currentHp;
            fillImage.color = normalColor;
            UpdateText(currentHp, maxHp);
        }

        // ─── 内部処理 ────────────────────────────────────────────────────

        private async UniTask SlideToAsync(int currentHp, int maxHp)
        {
            float from = hpSlider.value;
            float to = currentHp;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime; // ヒットストップ対応
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
                hpSlider.value = Mathf.Lerp(from, to, eased);
                UpdateText((int)hpSlider.value, maxHp);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            hpSlider.value = to;
            UpdateText(currentHp, maxHp);
        }

        private async UniTask ResetColorAsync()
        {
            float elapsed = 0f;

            while (elapsed < colorResetDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / colorResetDuration);
                fillImage.color = Color.Lerp(damagedColor, normalColor, t);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            fillImage.color = normalColor;
        }

        private void UpdateText(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{current}/{max}";
        }
    }
}