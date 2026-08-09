using UnityEditor;
using UnityEngine;

/// <summary>
/// ReadOnly 어트리뷰트가 붙은 변수를 인스펙터에서 비활성화 상태로 그려냅니다.
/// </summary>
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label);
    }
}
