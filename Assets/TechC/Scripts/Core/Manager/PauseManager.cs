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
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            pauseView.HidePauseMenu();
        }

        private void OnResume() => Resume();

        private void OnReturnToTitle()
        {
            Resume(); // timeScale を戻してからシーン遷移
            GameManager.I.LoadSceneAsync(0);
        }

        private void OnOpenSettings()
        {
            // TODO:SettingsView を開く
        }
    }
}