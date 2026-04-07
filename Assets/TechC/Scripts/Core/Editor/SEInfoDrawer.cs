using TechC.Core.Manager;
using UnityEditor;
using UnityEngine;

namespace TechC.Core.Editor
{
    /// <summary>
    /// AudioDataのSEElementをidの表記に
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioData.SEInfo))]
    public class SEInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var idProp = property.FindPropertyRelative("id");
            string name = idProp.enumDisplayNames[idProp.enumValueIndex];
            EditorGUI.PropertyField(position, property, new GUIContent(name), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
}
