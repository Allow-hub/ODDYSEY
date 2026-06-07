using System.IO;
using TechC.ODDESEY.Reward;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TechC.Core.Editor
{
    public static class ResultWorkSceneBuilder
    {
        private const string ScenePath = "Assets/TechC/Scenes/WorkScene/WorkScene_SR_Result.unity";
        private const string ResultPrefabPath = "Assets/TechC/Prefabs/Result.prefab";

        [MenuItem("Tools/TechC/Result/Create Result Work Scene")]
        public static void CreateResultWorkScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);

            if (resultPrefab == null)
            {
                Debug.LogError($"Result prefab not found: {ResultPrefabPath}");
                return;
            }

            GameObject resultRoot = (GameObject)PrefabUtility.InstantiatePrefab(resultPrefab, scene);
            resultRoot.name = "ResultPreviewRoot";

            ResultController resultController = resultRoot.GetComponent<ResultController>();
            GameObject bootstrapObject = new("ResultPreviewBootstrap");
            ResultPreviewBootstrap bootstrap = bootstrapObject.AddComponent<ResultPreviewBootstrap>();

            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            SerializedObject serializedBootstrap = new(bootstrap);
            serializedBootstrap.FindProperty("resultController").objectReferenceValue = resultController;
            serializedBootstrap.FindProperty("initializeOnStart").boolValue = true;
            serializedBootstrap.FindProperty("isCleared").boolValue = true;
            serializedBootstrap.FindProperty("previewRank").enumValueIndex = (int)TechC.ODDESEY.Result.Rank.A;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);

            Debug.Log($"Created result work scene: {ScenePath}");
        }
    }
}
