using UnityEngine;
using UnityEditor;

/// <summary>
/// 수정 못하게 막기
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyField))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
