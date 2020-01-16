using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomPropertyDrawer(typeof(UIAudioEvent))]
    public class UIAudioEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var totalWidth = position.width;
		
            position.width = 70;
            EditorGUI.LabelField(position, "Trigger on");
            position.x += 72;
        
            position.width = totalWidth - 70;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("TriggerType"), GUIContent.none);
            GUILayout.EndHorizontal();
        
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("AudioEvent"), GUIContent.none);
        }
    }
    
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
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events");
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.AudioEvents)
            {
                AsComponentBackup.RefreshEvent(evt.AudioEvent);   
            }
        }
    }
}