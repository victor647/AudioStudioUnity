using AudioStudio.Components;
using UnityEditor;
using UnityEngine;
using AudioStudio.Configs;
using AudioStudio.Tools;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioEvent)), CanEditMultipleObjects]
    public class AudioEventInspector : UnityEditor.Editor
    {
        protected static void RenameAsset(string newName, AudioEvent ae)
        {
            if (GUILayout.Button("Rename By Audio Clip"))
            {
                var assetPath = AssetDatabase.GetAssetPath(ae.GetInstanceID());
                AssetDatabase.RenameAsset(assetPath, newName);
            }
        }

        protected void DrawSubMixer(AudioEvent ae)
        {
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SubMixer"), "", 100, 10);
            if (ae.SubMixer)
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AudioMixer"), "    Mixer Name");
        }

        protected void DrawFilters(AudioEvent ae)
        {
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("LowPassFilter"), "Low Pass Filter");
            if (ae.LowPassFilter)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("    Cutoff", GUILayout.Width(70));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LowPassCutoff"), GUIContent.none, GUILayout.MinWidth(50));
                EditorGUILayout.LabelField("Resonance", GUILayout.Width(70));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LowPassResonance"), GUIContent.none, GUILayout.MinWidth(30));
                GUILayout.EndHorizontal();
            }

            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("HighPassFilter"));
            if (ae.HighPassFilter)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("    Cutoff", GUILayout.Width(70));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HighPassCutoff"), GUIContent.none, GUILayout.MinWidth(50));
                EditorGUILayout.LabelField("Resonance", GUILayout.Width(70));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HighPassResonance"), GUIContent.none, GUILayout.MinWidth(30));
                GUILayout.EndHorizontal();
            }
        }

        protected void DrawParameterMappings()
        {
            EditorGUILayout.LabelField("Map to AudioParameter(s)");
            AsGuiDrawer.DrawList(serializedObject.FindProperty("Mappings"));
        }
        
        protected void DrawAuditionButtons(AudioEvent ae)
        {
            if (!Application.isPlaying) return;
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Play", EditorStyles.miniButtonLeft))
                ae.Play(GlobalAudioEmitter.GameObject);
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Stop", EditorStyles.miniButtonRight))
                ae.Stop(GlobalAudioEmitter.GameObject);
            EditorGUILayout.EndHorizontal();
            GUI.contentColor = Color.white;
        }
    }
}