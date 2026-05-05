using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TechC.VBattle.Editor
{
    public class LightProbePlacerWindow : EditorWindow
    {
        private static LightProbeGroup targetGroup;

        private static Vector3 center = Vector3.zero;
        private static Vector3 size = new Vector3(10, 5, 10);
        private static float spacing = 2f;
        private static bool clearBeforeGenerate = true;
        private static bool showGizmo = true;

        [MenuItem("Tools/Light Probe Placer")]
        public static void ShowWindow()
        {
            GetWindow<LightProbePlacerWindow>("Light Probe Placer");
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            showGizmo = EditorGUILayout.Toggle("Show Gizmo", showGizmo);
            targetGroup = (LightProbeGroup)EditorGUILayout.ObjectField(
                "Target LightProbeGroup",
                targetGroup,
                typeof(LightProbeGroup),
                true
            );

            if (targetGroup == null)
            {
                EditorGUILayout.HelpBox("LightProbeGroup を設定してください。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            center = EditorGUILayout.Vector3Field("Center", center);
            size = EditorGUILayout.Vector3Field("Size", size);
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
            clearBeforeGenerate = EditorGUILayout.Toggle("Clear Before Generate", clearBeforeGenerate);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Light Probes", GUILayout.Height(30)))
            {
                GenerateProbes();
            }

            // SceneView の Gizmo 更新
            SceneView.RepaintAll();
        }

        private void GenerateProbes()
        {
            if (targetGroup == null)
                return;

            Undo.RecordObject(targetGroup, "Generate Light Probes");

            if (clearBeforeGenerate)
                targetGroup.probePositions = new Vector3[0];

            List<Vector3> positions = new List<Vector3>();

            Vector3 start = center - size * 0.5f;

            for (float x = 0; x <= size.x; x += spacing)
            {
                for (float y = 0; y <= size.y; y += spacing)
                {
                    for (float z = 0; z <= size.z; z += spacing)
                    {
                        positions.Add(start + new Vector3(x, y, z));
                    }
                }
            }

            targetGroup.probePositions = positions.ToArray();
            EditorUtility.SetDirty(targetGroup);

            Debug.Log($"[LightProbePlacer] {positions.Count} 個の Light Probe を生成しました。");

            SceneView.RepaintAll();
        }

        // ======================
        // ■ Gizmo 描画（Editor）
        // ======================
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawAreaGizmo(LightProbeGroup group, GizmoType gizmoType)
        {
            if (!showGizmo) return;

            if (group == targetGroup)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(group.transform.position + center, size);
            }
        }
    }
}
