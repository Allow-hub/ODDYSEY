using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TechC.Core.Manager
{
    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private int targetFrameRate = 144;
        protected override bool DontDestroy => true;
        public enum GameState
        {
            Title,
            Menu,
            Clear,
            GameOver
        }
        public GameState currentState = GameState.Title;
        protected override void OnInit()
        {
            base.OnInit();

            // VSyncCount を Dont Sync に変更
            QualitySettings.vSyncCount = 0;
            // fps 144 を目標に設定
            Application.targetFrameRate = targetFrameRate;
        }


        private void Update()
        {
            StateHandler();
        }

        private void SetState(GameState state)
        {
            currentState = state;
            //switch (state)
            //{
            //}
        }
        private void StateHandler()
        {
            //switch (currentState)
            //{

            //}
        }

        private void ChangeCursorMode(bool visible, CursorLockMode cursorLockMode)
        {
            Cursor.visible = visible;
            Cursor.lockState = cursorLockMode;
        }


        // 非同期でシーンをロード
        public void LoadSceneAsync(int sceneIndex)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex));
        }

        // 非同期でシーンをロードするコルーチン
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
                {
                    asyncOperation.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }

}