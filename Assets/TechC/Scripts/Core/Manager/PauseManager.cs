using TechC.Core.Manager;
using UnityEngine;

namespace TechC.ODDESEY.Core.Manager
{
    /// <summary>
    /// ポーズ機能を管理するマネージャー。
    /// </summary>
    public class PauseManager : Singleton<PauseManager>
    {
        [SerializeField] private PauseView pauseView;

        protected override bool DontDestroy => true;

        public bool IsPaused { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
            pauseView.Init();
            pauseView.InitializeView(OnResume, OnReturnToTitle, OnOpenSettings);
            pauseView.HidePauseMenu();
            pauseView.HideSettingsMenu();
            IsPaused = false;
        }

        /// <summary>ポーズを開く（外部・ボタンから呼ぶ）</summary>
        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            pauseView.ShowPauseMenu();
        }

        /// <summary>ポーズを閉じる</summary>
        public void Resume()
        {
            pauseView.HidePauseMenu();
            pauseView.HideSettingsMenu();
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
        }

        private void OnResume() => Resume();

        private void OnReturnToTitle()
        {
            pauseView.HidePauseMenu();
            pauseView.HideSettingsMenu();
            IsPaused = false;
            Time.timeScale = 1f;
            GameManager.I.LoadSceneAsync(0);
        }

        public void OnOpenSettings()
        {
            pauseView.HidePauseMenu();
            pauseView.ShowSettingsMenu();
        }
    }
}