using System;
using TechC.Core.Manager;
using TechC.ODDESEY.Core.Util;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY
{
    /// <summary>
    /// ポーズメニューの表示を担当する MonoBehaviour。
    /// PauseManager から呼び出され、ボタンのイベントを受け取る。
    /// </summary>
    public class PauseView : Singleton<PauseView>
    {
        [SerializeField] private CanvasGroup settingPanel;
        [SerializeField] private Button resumeSettingButton;

        [SerializeField] private CanvasGroup pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private Button settingsButton;

        protected override bool DontDestroy => false;

        public void InitializeView(Action onResume, Action onReturnToTitle, Action onOpenSettings)
        {
            resumeButton.onClick.AddListener(() => onResume?.Invoke());
            returnToTitleButton.onClick.AddListener(() => onReturnToTitle?.Invoke());
            settingsButton.onClick.AddListener(() => onOpenSettings?.Invoke());
        }

        public void ShowPauseMenu() => CanvasGroupUtil.SetInteractable(pausePanel, true);
        public void HidePauseMenu() => CanvasGroupUtil.SetInteractable(pausePanel, false);
        public void ShowSettingsMenu() => CanvasGroupUtil.SetInteractable(settingPanel, true);
        public void HideSettingsMenu() => CanvasGroupUtil.SetInteractable(settingPanel, false);
    }
}