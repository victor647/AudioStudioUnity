using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(EmitterSound)), CanEditMultipleObjects]
    public class EmitterSoundInspector : AsComponentInspector
    {
        private EmitterSound _component;

        private void OnEnable()
        {
            _component = target as EmitterSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AudioScriptGUI.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events:", AddEvent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeInTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeOutTime"));
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.AudioEvents, new AudioEventReference(evt.name));
            }
        }
        
        protected override void Refresh()
        {
            foreach (var evt in _component.AudioEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
        }
    }
}