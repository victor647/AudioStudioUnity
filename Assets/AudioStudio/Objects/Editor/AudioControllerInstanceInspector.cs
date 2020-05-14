using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioSwitchInstance)), CanEditMultipleObjects]
    public class AudioSwitchInstanceInspector : UnityEditor.Editor
    {
        private AudioSwitchInstance _component;

        private void OnEnable()
        {
            _component = target as AudioSwitchInstance;
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioSwitch"));
            GUI.enabled = true;
        }
    }
}