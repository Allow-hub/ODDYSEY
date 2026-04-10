using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TechC.Core.Managers
{
    /// <summary>
    /// ManagerSceneを自動的に読み込むためのクラス
    /// どのシーンから再生しても、ManagerScene(Awake/Start) → 元シーン(Awake/Start) の順で初期化される
    /// </summary>
    public class ManagerSceneAutoLoader
    {
        private const string managerSceneName = "ManagerScene";
        private static string originalSceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // 現在のシーン名を取得
            originalSceneName = SceneManager.GetActiveScene().name;

            // ManagerSceneでない場合のみ処理
            if (originalSceneName != managerSceneName)
            {
                // ManagerSceneを非同期で読み込む
                SceneManager.LoadSceneAsync(managerSceneName, LoadSceneMode.Single).completed += OnManagerSceneLoaded;
            }
        }

        private static void OnManagerSceneLoaded(AsyncOperation operation)
        {
            // ManagerSceneのAwake/Startが完了した後、元のシーンを非同期で読み込む
            SceneManager.LoadSceneAsync(originalSceneName, LoadSceneMode.Additive).completed += OnOriginalSceneLoaded;
        }

        private static void OnOriginalSceneLoaded(AsyncOperation operation)
        {
            // 元のシーンをアクティブに設定
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(originalSceneName));
            
            // ManagerSceneをアンロード
            SceneManager.UnloadSceneAsync(managerSceneName);
        }
    }
}