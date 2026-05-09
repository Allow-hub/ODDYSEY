// using UnityEditor;
// using UnityEngine;
// using UnityEngine.VFX;

// namespace TechC.VBattle.Editor
// {
//     [CustomEditor(typeof(Transform), true)]
//     public class VFXHierarchyInspector : UnityEditor.Editor
//     {
//         // Inspector 拡張を有効化するか
//         private const string EnableKey = "VFXHierarchyInspector_Enable";

//         private static bool IsEnabled
//         {
//             get => EditorPrefs.GetBool(EnableKey, true);
//             set => EditorPrefs.SetBool(EnableKey, value);
//         }

//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();

//             EditorGUILayout.Space();

//             // =========================
//             // 有効 / 無効
//             // =========================
//             bool newEnabled = EditorGUILayout.ToggleLeft(
//                 "Enable VFX Graph Tools",
//                 IsEnabled);

//             if (newEnabled != IsEnabled)
//             {
//                 IsEnabled = newEnabled;
//             }

//             // 無効ならここで終了
//             if (!IsEnabled)
//                 return;

//             Transform targetTransform = (Transform)target;

//             // 子階層含め VFX を検索
//             var vfxList = targetTransform.GetComponentsInChildren<VisualEffect>(true);

//             if (vfxList.Length == 0)
//                 return;

//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("VFX Graph Tools", EditorStyles.boldLabel);

//             EditorGUILayout.BeginHorizontal();

//             if (GUILayout.Button("Play"))
//             {
//                 Execute(vfxList, vfx =>
//                 {
//                     vfx.Play();
//                 });
//             }

//             if (GUILayout.Button("Stop"))
//             {
//                 Execute(vfxList, vfx =>
//                 {
//                     vfx.Stop();
//                 });
//             }

//             if (GUILayout.Button("Restart"))
//             {
//                 Execute(vfxList, vfx =>
//                 {
//                     vfx.Reinit();
//                     vfx.Play();
//                 });
//             }

//             EditorGUILayout.EndHorizontal();
//         }

//         private void Execute(
//             VisualEffect[] vfxList,
//             System.Action<VisualEffect> action)
//         {
//             foreach (var vfx in vfxList)
//             {
//                 if (vfx == null)
//                     continue;

//                 action.Invoke(vfx);

//                 EditorUtility.SetDirty(vfx);
//             }

//             SceneView.RepaintAll();

//             Debug.Log($"[VFX] {vfxList.Length} 個の VFX を処理");
//         }
//     }
// }