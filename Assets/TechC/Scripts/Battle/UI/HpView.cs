using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 敵とプレイヤー共用のHP表示クラス
    /// </summary>
    public class HpView : MonoBehaviour
    {
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Color")]
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color damagedColor = Color.red;
        [SerializeField] private float colorResetDuration = 0.5f;

        [Header("Animation")]
        [SerializeField] private float slideDuration = 0.3f;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="maxHp">最大HP</param>
        public void Setup(int maxHp)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
            fillImage.color = normalColor;
            UpdateText(maxHp, maxHp);
        }

        /// <summary>
        /// スライダーの色を更新する
        /// </summary>
        /// <param name="currentHp">現在のHP</param>
        /// <param name="maxHp"></param>
        /// <returns></returns>
        public async UniTask UpdateHpAsync(int currentHp, int maxHp)
        {
            // 色をダメージ色に変えてから元に戻す
            fillImage.color = damagedColor;

            // スライダーをアニメーション
            await SlideToAsync(currentHp, maxHp);

            // 色をリセット
            await ResetColorAsync();
        }

        /// <summary>
        /// スライダーの数値をアニメーションする
        /// </summary>
        /// <param name="currentHp">現在のHP</param>
        /// <param name="maxHp">最大HP</param>
        /// <returns></returns>
        private async UniTask SlideToAsync(int currentHp, int maxHp)
        {
            float from = hpSlider.value;
            float to = currentHp;
            float t = 0;

            while (t < slideDuration)
            {
                t += Time.deltaTime;
                hpSlider.value = Mathf.Lerp(from, to, t / slideDuration);
                UpdateText((int)hpSlider.value, maxHp);
                await UniTask.Yield();
            }

            hpSlider.value = to;
        }

        /// <summary>
        /// 色を元に戻す
        /// </summary>
        /// <returns></returns>
        private async UniTask ResetColorAsync()
        {
            float t = 0;
            while (t < colorResetDuration)
            {
                t += Time.deltaTime;
                fillImage.color = Color.Lerp(damagedColor, normalColor, t / colorResetDuration);
                await UniTask.Yield();
            }
            fillImage.color = normalColor;
        }

        /// <summary>
        /// HPのテキストの更新
        /// </summary>
        /// <param name="current">現在の値</param>
        /// <param name="max">最大値</param>
        private void UpdateText(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{current}/{max}";
        }
    }
}