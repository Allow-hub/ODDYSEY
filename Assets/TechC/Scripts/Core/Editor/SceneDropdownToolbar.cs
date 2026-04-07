using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TechC.Core.Editor
{
    [InitializeOnLoad]
    public static class SceneDropdownToolbar
    {
        private static VisualElement rootVisualElement;
        private static VisualElement leftZoneAlign;
        private static string[] scenePaths;
        private static string[] sceneNames;

        static SceneDropdownToolbar()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (rootVisualElement != null) return;
            RebuildToolbar();
        }

        private static void RebuildToolbar()
        {
            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar"); var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            var currentToolbar = toolbars.FirstOrDefault();
            if (currentToolbar == null) return;

            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            var fieldInfo = toolbarType.GetField("m_Root", bindingFlags);

            rootVisualElement = fieldInfo.GetValue(currentToolbar) as VisualElement;
            if (rootVisualElement == null) return;

            leftZoneAlign = rootVisualElement.Q("ToolbarZoneLeftAlign");
            if (leftZoneAlign == null) return;

            LoadScenes();
            AddSceneIconButton();
        }

        /// <summary>
        /// シーンアイコンを描画
        /// </summary>
        private static void AddSceneIconButton()
        {
            if (sceneNames.Length == 0) return;

            // IMGUIContainer ボタンを作成
            IMGUIContainer container = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
                if (GUILayout.Button(EditorGUIUtility.IconContent("SceneAsset Icon"), GUILayout.Width(30), GUILayout.Height(22)))
                    ShowSceneMenu();

                GUILayout.EndHorizontal();
            });

            container.style.width = 30;
            container.style.height = 22;
            container.style.marginLeft = 2;
            container.style.marginRight = 2;

            // 再生ボタンの複合要素を探す
            VisualElement playModeTools = rootVisualElement.Query<VisualElement>()
                .Where(e => e.name == "PlayMode")//見つからない場合はエレメントの要素を再帰的にログ出せば見つかります
                .First();

            if (playModeTools != null)
            {
                var parent = playModeTools.parent;
                int index = parent.IndexOf(playModeTools);
                parent.Insert(index, container); // 再生ボタンの左に追加
            }
            else
                leftZoneAlign.Add(container);// 見つからなければ左端に追加
        }

        /// <summary>
        /// 選択できるシーンの表示
        /// </summary>
        private static void ShowSceneMenu()
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < sceneNames.Length; i++)
            {
                string name = sceneNames[i];
                string path = scenePaths[i];
                menu.AddItem(new GUIContent(name), false, () =>
                {
                    if (Application.isPlaying)
                        SceneManager.LoadScene(path);// 再生中：普通にロード
                    else
                    {
                        // 編集モード：シーンを保存してから切り替え
                        bool saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        if (!saved) return; // キャンセルされたら中断

                        EditorSceneManager.OpenScene(path);
                    }
                });
            }
            menu.DropDown(new Rect(0, 20, 0, 0));
        }

        /// <summary>
        /// 特定のフォルダ以下のSceneのguidsを取得
        /// </summary>
        private static void LoadScenes()
        {
            //適したフォルダ階層にシーンを配置している前提で、シーンのパスと名前を取得
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/TechC/Scenes" });
            scenePaths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).Where(p => p.EndsWith(".unity")).ToArray();
            sceneNames = scenePaths.Select(p => System.IO.Path.GetFileNameWithoutExtension(p)).ToArray();
        }
    }
}