using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(MusicTransitionSegment)), CanEditMultipleObjects]
    public class MusicTransitionSegmentInspector : MusicTrackInspector
    {
        private MusicTransitionSegment _musicTransitionSegment;

        private void OnEnable()
        {
            _musicTransitionSegment = target as MusicTransitionSegment;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawProperty("Clip", "Audio Clip", 80);            
            DrawRhythm();
            DrawTransitionSettings();
            DrawAudioControls(_musicTransitionSegment);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawTransitionSettings()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Origin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Destination"));
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fade out from origin");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OriginFadeOutTime"), GUIContent.none, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fade in from origin");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SegmentFadeInTime"), GUIContent.none, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fade out to destination");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SegmentFadeOutTime"), GUIContent.none, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Fade in to destination");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DestinationFadeInTime"), GUIContent.none, GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }
        }
    }
}