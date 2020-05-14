using System.Linq;
using UnityEditor;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioInitSettings))]
    public class AudioInitSettingsInspector : UnityEditor.Editor
    {
        private AudioInitSettings _component;

        private void OnEnable()
        {
            _component = target as AudioInitSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DisableAudio"));
            if (!_component.DisableAudio)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DebugLogLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PathSettings"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioMixer"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UseMicrophone"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("UseMidi"));
            }
            AsGuiDrawer.DrawSaveButton(_component);
            serializedObject.ApplyModifiedProperties();
        }
    }
}