using UnityEditor;
using UnityEngine;

namespace TechC.Core.Editor
{
    /// <summary>
    /// ReadOnlyAttribute のためのプロパティドローワー
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // GUI を無効化
            bool previousGUIState = GUI.enabled;
            GUI.enabled = false;

            // プロパティを通常通り描画（ただし編集不可）
            EditorGUI.PropertyField(position, property, label);

            // GUI の状態を元に戻す
            GUI.enabled = previousGUIState;
        }
    }
}