using System.Collections;
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
        /// 非同期でシーンをロード
        /// </summary>
        /// <param name="sceneIndex"></param>
        public void LoadSceneAsync(int sceneIndex)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }

        /// <summary>
        /// 非同期でシーンをロードするコルーチン
        /// </summary>
        /// <param name="sceneIndex"></param>
        /// <returns></returns>
        private IEnumerator LoadSceneCoroutine(int sceneIndex)
        {
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
            asyncOperation.allowSceneActivation = false;

            // シーンのロードが終わるまで待機
            while (!asyncOperation.isDone)
            {
                // ロードが進んだら進行状況を表示
                float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
                Debug.Log("Loading progress: " + (progress * 100) + "%");

                // ロードが完了したらシーンをアクティブ化
                if (asyncOperation.progress >= 0.9f)
                    asyncOperation.allowSceneActivation = true;

                yield return null;
            }
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