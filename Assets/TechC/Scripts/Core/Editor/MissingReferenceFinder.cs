using UnityEngine;
using UnityEditor;

namespace TechC.Core.Editor
{
    /// <summary>
    /// シーン内のMissingなScript・Prefab・参照を検索するツール
    /// </summary>
    public class MissingReferenceFinder : EditorWindow
    {
        private Vector2 scrollPos;
        private string result = "";

        [MenuItem("Tools/Missing Reference Finder")]
        public static void ShowWindow()
        {
            GetWindow<MissingReferenceFinder>("Missing Reference Finder");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Missingを検索"))
            {
                FindMissingReferences();
            }

            EditorGUILayout.LabelField("検索結果:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(result);
            EditorGUILayout.EndScrollView();
        }

        private void FindMissingReferences()
        {
            result = "";
            int goCount = 0;
            int compCount = 0;
            int missingScriptCount = 0;
            int missingRefCount = 0;

            GameObject[] allGOs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject go in allGOs)
            {
                goCount++;
                Component[] components = go.GetComponents<Component>();
                compCount += components.Length;

                for (int i = 0; i < components.Length; i++)
                {
                    Component comp = components[i];

                    // Missing Script
                    if (comp == null)
                    {
                        missingScriptCount++;
                        string path = GetGameObjectPath(go);
                        string log = $"[Missing Script] {path}\n";
                        Debug.LogWarning(log, go);
                        result += log;
                        continue;
                    }

                    // SerializedObjectを使って参照チェック
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty prop = so.GetIterator();

                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                            {
                                missingRefCount++;
                                string path = GetGameObjectPath(go);
                                string log = $"[Missing Reference] {path} → {prop.displayName}\n";
                                Debug.LogWarning(log, go);
                                result += log;
                            }
                        }
                    }
                }

                // Missing Prefab（Prefabが壊れている場合）
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                    if (prefabStatus != PrefabInstanceStatus.Connected)
                    {
                        string path = GetGameObjectPath(go);
                        string log = $"[Missing Prefab] {path}\n";
                        Debug.LogWarning(log, go);
                        result += log;
                    }
                }
            }

            result += $"\n総GameObject数: {goCount}\n";
            result += $"総Component数: {compCount}\n";
            result += $"Missing Script数: {missingScriptCount}\n";
            result += $"Missing Reference数: {missingRefCount}\n";
        }

        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}
