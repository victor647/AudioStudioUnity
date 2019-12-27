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
            ShowSpatialSettings();
            ShowPlaybackSettings();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events:", AddEvent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeOutTime"));
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }
        
        private void ShowPlaybackSettings()
        {
            EditorGUILayout.LabelField("Playback Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayMode"));
                if (_component.PlayMode == EventPlayMode.PeriodTrigger)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("InitialDelay"));
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Trigger Interval", GUILayout.Width(116));
                    EditorGUILayout.LabelField("Min", GUILayout.Width(30));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MinInterval"), GUIContent.none, GUILayout.Width(30));
                    EditorGUILayout.LabelField("Max", GUILayout.Width(30));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxInterval"), GUIContent.none, GUILayout.Width(30));
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void AddEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.AudioEvents, new PostEventReference(evt.name));
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