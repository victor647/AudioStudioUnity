using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(EventSound)), CanEditMultipleObjects]
    public class EventSoundInspector : AsComponentInspector
    {
        private EventSound _component;

        private void OnEnable()
        {
            _component = target as EventSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("UIAudioEvents"), "Audio Events");
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.UIAudioEvents)
            {
                AsComponentBackup.RefreshEvent(evt.AudioEvent);   
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(UIAudioEvent))]
    public class UIAudioEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("TriggerType"), GUIContent.none);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("AudioEvent"), GUIContent.none);
        }
    }
}