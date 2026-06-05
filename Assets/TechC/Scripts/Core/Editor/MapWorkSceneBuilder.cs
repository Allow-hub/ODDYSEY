using System.IO;
using TechC.ODDESEY.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TechC.Core.Editor
{
    public static class MapWorkSceneBuilder
    {
        private const string ScenePath = "Assets/TechC/Scenes/WorkScene/WorkScene_SR_Map.unity";
        private const string MapPrefabPath = "Assets/TechC/Prefabs/Map/Map.prefab";
        private const string StageMapDataPath = "Assets/TechC/Data/Map/StageMapData.asset";

        [MenuItem("Tools/TechC/Map/Create Map Work Scene")]
        public static void CreateMapWorkScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapPrefabPath);
            StageMapData stageMapData = AssetDatabase.LoadAssetAtPath<StageMapData>(StageMapDataPath);

            if (mapPrefab == null)
            {
                Debug.LogError($"Map prefab not found: {MapPrefabPath}");
                return;
            }

            GameObject mapRoot = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
            mapRoot.name = "MapPreviewRoot";

            GameObject bootstrapObject = new("MapPreviewBootstrap");
            MapPreviewBootstrap bootstrap = bootstrapObject.AddComponent<MapPreviewBootstrap>();
            MapController mapController = mapRoot.GetComponent<MapController>();

            SerializedObject serializedBootstrap = new(bootstrap);
            serializedBootstrap.FindProperty("mapController").objectReferenceValue = mapController;
            serializedBootstrap.FindProperty("stageMapData").objectReferenceValue = stageMapData;
            serializedBootstrap.FindProperty("currentNodeIndex").intValue = 0;
            serializedBootstrap.FindProperty("initializeOnStart").boolValue = true;
            serializedBootstrap.FindProperty("refreshAfterSelection").boolValue = true;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Selection.activeObject = SceneAssetFromPath(ScenePath);

            Debug.Log($"Created map work scene: {ScenePath}");
        }

        private static SceneAsset SceneAssetFromPath(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }
    }
}
