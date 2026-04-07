using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TechC.Core.Editor
{
    public class FindScriptObjectsWindow : EditorWindow
    {
        private MonoScript targetScript; // 検索対象のスクリプト
        private List<GameObject> foundObjects = new List<GameObject>(); // 見つかったオブジェクトのリスト

        [MenuItem("Tools/Find Script Objects")]
        public static void OpenWindow()
        {
            GetWindow<FindScriptObjectsWindow>("Find Script Objects");
        }

        private void OnGUI()
        {
            GUILayout.Label("Find Objects with Script", EditorStyles.boldLabel);

            // スクリプトを選択するフィールド
            targetScript = (MonoScript)EditorGUILayout.ObjectField("Script", targetScript, typeof(MonoScript), false);

            if (GUILayout.Button("Find"))
            {
                if (targetScript != null)
                {
                    FindObjectsWithScript();
                }
                else
                {
                    Debug.LogWarning("Please select a script to search for.");
                }
            }

            GUILayout.Space(10);

            // 検索結果のリスト表示
            if (foundObjects.Count > 0)
            {
                GUILayout.Label($"Found {foundObjects.Count} objects:", EditorStyles.boldLabel);

                foreach (GameObject obj in foundObjects)
                {
                    if (GUILayout.Button(obj.name))
                    {
                        Selection.activeGameObject = obj; // オブジェクトを選択
                    }
                }
            }
        }

        private void FindObjectsWithScript()
        {
            foundObjects.Clear();

            // スクリプトの型を取得
            System.Type scriptType = targetScript.GetClass();
            if (scriptType == null || !typeof(MonoBehaviour).IsAssignableFrom(scriptType))
            {
                Debug.LogError("Selected script is not a MonoBehaviour.");
                return;
            }

            // シーン内の全てのGameObjectを検索
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (obj.GetComponent(scriptType) != null)
                {
                    foundObjects.Add(obj);
                }
            }

            Debug.Log($"Found {foundObjects.Count} objects with the script {targetScript.name}.");
        }
    }
}
