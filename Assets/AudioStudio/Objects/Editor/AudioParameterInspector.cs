using UnityEngine;
using UnityEditor;
using AudioStudio.Configs;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioParameter)), CanEditMultipleObjects]
    public class AudioParameterInspector : UnityEditor.Editor
    {
        private AudioParameter _audioParameter;

        private void OnEnable()
        {
            _audioParameter = target as AudioParameter;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Value Range:", GUILayout.Width(100));
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MinValue"), GUIContent.none, GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxValue"), GUIContent.none, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Value", GUILayout.Width(100));
            _audioParameter.DefaultValue = EditorGUILayout.Slider(_audioParameter.DefaultValue, _audioParameter.MinValue, _audioParameter.MaxValue);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Slew", GUILayout.Width(100));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Slew"), GUIContent.none, GUILayout.Width(30));
            if (_audioParameter.Slew)
            {
                EditorGUILayout.LabelField("Rate", GUILayout.Width(50));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SlewRate"), GUIContent.none, GUILayout.Width(50));
            }

            GUILayout.EndHorizontal();

            AsGuiDrawer.DrawSaveButton(_audioParameter);
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomPropertyDrawer(typeof(ParameterMapping))]
    public class ParameterMappingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var fullWidth = position.width;

            position.width = fullWidth * 0.3f;
            EditorGUI.LabelField(position, "Parameter");

            position.x += position.width;
            position.width = fullWidth * 0.7f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("AudioParameterReference"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mapping Target", GUILayout.Width(100));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Target"), GUIContent.none, GUILayout.MinWidth(100));
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Parameter Value Map to Target Value:");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MinParameterValue"), GUIContent.none, GUILayout.MinWidth(40));
            EditorGUILayout.LabelField("map to", GUILayout.Width(50));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MinTargetValue"), GUIContent.none, GUILayout.MinWidth(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxParameterValue"), GUIContent.none, GUILayout.MinWidth(40));
            EditorGUILayout.LabelField("map to", GUILayout.Width(50));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("MaxTargetValue"), GUIContent.none, GUILayout.MinWidth(40));
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("CurveExponent"));
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(" ");
        }
    }
}