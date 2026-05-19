using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// シールド量の表示を管理する View。
    ///
    /// BattleView から PlayCardResolveAsync の演出タイミングに合わせて
    /// ShowShield / UpdateShield / HideShield を呼ぶ。
    /// ShieldModel のイベントは使わない（ロジックが先に完了するため）。
    /// </summary>
    public class ShieldView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI shieldText;

        [Header("フェード設定")]
        [SerializeField] private float fadeDuration = 0.15f;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        // ─── 公開API（BattleView から呼ぶ）──────────────────────────────

        /// <summary>
        /// シールドを付与演出付きで表示する。
        /// ShieldEffect 解決後に呼ぶ。
        /// </summary>
        public async UniTask ShowShieldAsync(int value)
        {
            SetText(value);
            await FadeAsync(canvasGroup.alpha, 1f);
                Debug.Log($"[ShieldView] ShowShieldAsync value={value} alpha={canvasGroup?.alpha}");

        }

        /// <summary>
        /// シールド量を即時更新する（フェードなし）。
        /// ダメージ吸収後の確定値を反映するときに呼ぶ。
        /// </summary>
        public void UpdateShield(int value)
        {
            SetText(value);
            if (value <= 0)
                HideShieldAsync().Forget();
        }

        /// <summary>
        /// シールド量を更新する（UniTask 版）。WhenAll から呼ぶ。
        /// 0 になったらフェードアウト、それ以外は即時更新。
        /// </summary>
        public UniTask UpdateShieldAsync(int value)
        {
            SetText(value);
            return value <= 0 ? HideShieldAsync() : UniTask.CompletedTask;
        }

        /// <summary>
        /// シールドをフェードアウトして非表示にする。
        /// シールドが 0 になったとき呼ぶ。
        /// </summary>
        public async UniTask HideShieldAsync()
        {
            await FadeAsync(canvasGroup.alpha, 0f);
        }

        /// <summary>
        /// アニメーションなしで即時セット（バトル開始・初期化時）。
        /// </summary>
        public void SetImmediate(int value)
        {
            SetText(value);
            canvasGroup.alpha = value > 0 ? 1f : 0f;
        }

        // ─── 内部処理 ────────────────────────────────────────────────────

        private async UniTask FadeAsync(float from, float to)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to,
                                         Mathf.Clamp01(elapsed / fadeDuration));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            canvasGroup.alpha = to;
        }

        private void SetText(int value)
        {
            if (shieldText != null)
                shieldText.text = value.ToString();
        }
    }
}