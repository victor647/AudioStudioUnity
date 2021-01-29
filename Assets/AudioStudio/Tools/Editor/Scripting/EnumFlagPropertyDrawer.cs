using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EnumFlagAttribute))]
public class EnumFlagPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var flagAttribute = (EnumFlagAttribute)attribute;

#if UNITY_2017_3_OR_NEWER
        property.longValue = EditorGUI.EnumFlagsField(position, new GUIContent(label.text), (System.Enum)System.Enum.ToObject(flagAttribute.Type, property.longValue)).GetHashCode();
#else
        property.longValue = EditorGUI.EnumMaskField(position, label, (System.Enum)System.Enum.ToObject(flagAttribute.Type, property.longValue)).GetHashCode();
#endif
        EditorGUI.EndProperty();
    }
}