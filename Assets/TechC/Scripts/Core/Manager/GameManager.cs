using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TechC.Core.Manager
{
    /// <summary>
    /// ゲーム全体の管理を行うクラス
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private int targetFrameRate = 144;
        protected override bool DontDestroy => true;

        public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

        protected override void OnInit()
        {
            base.OnInit();

            // VSyncCount を Dont Sync に変更
            QualitySettings.vSyncCount = 0;
            // fps 144 を目標に設定
            Application.targetFrameRate = targetFrameRate;
        }

        private void ChangeCursorMode(bool visible, CursorLockMode cursorLockMode)
        {
            Cursor.visible = visible;
            Cursor.lockState = cursorLockMode;
        }

        /// <summary>
        /// 非同期でシーンをロード（フェードイン・アウト付き）
        /// </summary>
        public void LoadSceneAsync(int sceneIndex)
        {
            LoadSceneWithFadeAsync(sceneIndex).Forget();
        }

        private async UniTaskVoid LoadSceneWithFadeAsync(int sceneIndex)
        {
            if (FadeManager.IsValid())
                await FadeManager.I.FadeOutAsync();

            var asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
            asyncOperation.allowSceneActivation = false;

            while (asyncOperation.progress < 0.9f)
                await UniTask.Yield(PlayerLoopTiming.Update);

            asyncOperation.allowSceneActivation = true;
            await UniTask.WaitUntil(() => asyncOperation.isDone);

            if (FadeManager.IsValid())
                await FadeManager.I.FadeInAsync();
        }

        /// <summary> ゲームの難易度を設定するメソッド</summary>
        /// <param name="difficulty">難易度</param>
        public void SetDifficulty(Difficulty difficulty) => CurrentDifficulty = difficulty;
    }

    public enum Difficulty
    {
        Normal,
        Hard
    }
}