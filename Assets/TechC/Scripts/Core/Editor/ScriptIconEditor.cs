
using UnityEditor;
using UnityEngine;

namespace TechC.Core.Editor
{
    [InitializeOnLoad]  // エディタが初期化される際に自動で実行
    public static class ScriptIconEditor
    {
        // 静的コンストラクタ。エディタが初期化される際に自動で実行
        static ScriptIconEditor()
        {
            // ヒエラルキーアイテムが描画されるたびに呼ばれる
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            // ヒエラルキーのオブジェクトを取得
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (obj == null) return;  // Nullチェック

            // ゲームオブジェクトにアタッチされたコンポーネントをすべて取得
            Component[] components = obj.GetComponents<Component>();

            foreach (var component in components)
            {
                // MonoBehaviour（スクリプトコンポーネント）を対象とする
                if (component is MonoBehaviour)
                {
                    // スクリプトのアイコンを取得
                    MonoScript script = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
                    Texture icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(script));

                    if (icon != null)
                    {
                        // アイコンをオブジェクト名の隣に表示する位置
                        Rect iconRect = new Rect(selectionRect.x + selectionRect.width - 20, selectionRect.y, 20, 20);

                        // アイコンを表示
                        GUI.Label(iconRect, icon);
                    }
                    break;  // 一つのコンポーネントにアイコンを表示したらループを抜ける
                }
            }
        }
    }
}
