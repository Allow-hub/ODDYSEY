// using UnityEditor;
// using UnityEngine;
// using UnityEngine.VFX;

// namespace TechC.VBattle.Editor
// {
//     public static class VFXGraphEditorExtensions
//     {
//         [MenuItem("GameObject/VFX/Play All Child VFX", false, 10)]
//         private static void PlayAllChildVFX()
//         {
//             Execute(vfx => vfx.Play());
//         }

//         [MenuItem("GameObject/VFX/Stop All Child VFX", false, 11)]
//         private static void StopAllChildVFX()
//         {
//             Execute(vfx => vfx.Stop());
//         }

//         [MenuItem("GameObject/VFX/Reinit All Child VFX", false, 12)]
//         private static void ReinitAllChildVFX()
//         {
//             Execute(vfx => vfx.Reinit());
//         }

//         [MenuItem("GameObject/VFX/Restart All Child VFX", false, 13)]
//         private static void RestartAllChildVFX()
//         {
//             Execute(vfx =>
//             {
//                 vfx.Reinit();
//                 vfx.Play();
//             });
//         }

//         private static void Execute(System.Action<VisualEffect> action)
//         {
//             if (Selection.activeGameObject == null)
//             {
//                 Debug.LogWarning("GameObject を選択してください");
//                 return;
//             }

//             var root = Selection.activeGameObject;

//             var vfxList = root.GetComponentsInChildren<VisualEffect>(true);

//             foreach (var vfx in vfxList)
//             {
//                 if (vfx == null)
//                     continue;

//                 action.Invoke(vfx);

//                 EditorUtility.SetDirty(vfx);
//             }

//             Debug.Log($"[VFX] {vfxList.Length} 個の VisualEffect を処理しました");
//         }
//     }
// }