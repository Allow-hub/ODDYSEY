using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TechC.Core.Managers
{
    /// <summary>
    /// ManagerSceneを自動的に読み込むためのクラス
    /// </summary>
    public class ManagerSceneAutoLoader
    {
        private const string managerSceneName = "ManagerScene";
        private static string originalSceneName;
        private static bool isProcessing = false;

        // シーン読み込み前に実行
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            if (isProcessing) return;
            isProcessing = true;

            // 現在のシーン名を取得
            Scene currentScene = SceneManager.GetActiveScene();
            originalSceneName = currentScene.name;

            // ManagerSceneでない場合のみ処理
            if (originalSceneName != managerSceneName)
            {
                // シーン読み込み完了イベントに登録
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                // 最初にManagerSceneを読み込む
                SceneManager.LoadScene(managerSceneName);
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == managerSceneName)
            {
                // ManagerSceneが読み込まれた後、コルーチンを開始
                GameObject obj = new GameObject("ManagerSceneAutoLoader");
                Object.DontDestroyOnLoad(obj);
                obj.AddComponent<ManagerSceneAutoLoaderBehaviour>().StartCoroutine(LoadOriginalSceneAfterInit());
                
                // イベント解除
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private static IEnumerator LoadOriginalSceneAfterInit()
        {
            // 1フレーム待機（ManagerSceneのAwakeが実行される）
            yield return null;

            // もう1フレーム待機（ManagerSceneのStartが実行される）
            yield return null;

            // 元のシーンを追加で読み込む
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(originalSceneName, LoadSceneMode.Additive);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 元のシーンをアクティブにする
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(originalSceneName));

            // 自身を削除
            Object.Destroy(GameObject.Find("ManagerSceneAutoLoader"));
        }
    }

    // コルーチン実行用のMonoBehaviour
    public class ManagerSceneAutoLoaderBehaviour : MonoBehaviour { }
}