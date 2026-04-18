using UnityEngine;

namespace TechC.ODDESEY.Core.Util
{
    public static class CanvasGroupUtil
    {
        /// <summary>
        /// CanvasGroup を使ってインタラクションを有効/無効にするユーティリティ。
        /// alpha = 0 で見えなくし、blocksRaycasts = false でクリックを透過させる。
        /// </summary>
        public static void SetInteractable(CanvasGroup canvasGroup, bool interactable)
        {
            canvasGroup.alpha = interactable ? 1f : 0f;
            canvasGroup.blocksRaycasts = interactable;
            canvasGroup.interactable = interactable;
        }
    }
}
