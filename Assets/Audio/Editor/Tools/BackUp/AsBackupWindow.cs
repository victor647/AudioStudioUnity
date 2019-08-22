using System;
using System.Collections.Generic;
using AudioStudio.Editor;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class AsBackupWindow : AsSearchers
    {	
        private void OnGUI()
        {
            AudioScriptGUI.DisplaySearchPath(ref SearchPath);
            DrawComponents();
            DrawAnimationEvents();
            DrawAudioStates();
            DrawTimeline();
        }

        private void DrawComponents()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Components in Prefabs and Scenes", EditorStyles.boldLabel);
            
            ShowToggles();

            EditorGUILayout.BeginHorizontal();                  
            EditorGUILayout.LabelField("Search Inclusion", GUILayout.Width(150));
            DrawToggle(ref AsComponentBackup.Instance.IncludeA, "Prefabs");
            DrawToggle(ref AsComponentBackup.Instance.IncludeB, "Scenes");
            EditorGUILayout.EndHorizontal();
            DrawToggle(ref AsComponentBackup.Instance.SeparateXmlFiles, "Create one xml per component");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", EditorStyles.toolbarButton)) 
                AsComponentBackup.Instance.Export();
            if (GUILayout.Button("Import", EditorStyles.toolbarButton)) 
                AsComponentBackup.Instance.Import();
            if (GUILayout.Button("Compare", EditorStyles.toolbarButton)) 
                AsComponentBackup.Instance.Compare();
            if (GUILayout.Button("Combine", EditorStyles.toolbarButton)) 
                AsComponentBackup.Instance.Combine();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton)) 
                AsComponentBackup.Instance.OpenXmlFile();
            EditorGUILayout.EndHorizontal();
        }
        
        private void ShowToggles()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var selections = new Dictionary<Type, bool>(AsComponentBackup.Instance.ComponentsInSearch);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft))
                {
                    foreach (var selection in selections)
                    {
                        AsComponentBackup.Instance.ComponentsInSearch[selection.Key] = true;
                    }
                }
                if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight))
                {
                    foreach (var selection in selections)
                    {
                        AsComponentBackup.Instance.ComponentsInSearch[selection.Key] = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                var count = 0;
                EditorGUILayout.BeginHorizontal();
                foreach (var selection in selections)
                {
                    var selected = GUILayout.Toggle(selection.Value, selection.Key.Name, GUILayout.Width(150));
                    if (selected != selection.Value)
                        AsComponentBackup.Instance.ComponentsInSearch[selection.Key] = selected;
                    count++;
                    if (count % 3 == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAnimationEvents()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Animation Events in Animation Clips and Models", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search Inclusion", GUILayout.Width(150));
            DrawToggle(ref AsAnimationEventBackup.Instance.IncludeA, ".anim");
            DrawToggle(ref AsAnimationEventBackup.Instance.IncludeA, ".fbx");
            DrawToggle(ref AsAnimationEventBackup.Instance.IncludeNonAudioEvents, "Non Audio Events");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", EditorStyles.toolbarButton)) 
                AsAnimationEventBackup.Instance.Export();
            if (GUILayout.Button("Import", EditorStyles.toolbarButton)) 
                AsAnimationEventBackup.Instance.Import();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton)) 
                AsAnimationEventBackup.Instance.OpenXmlFile();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAudioStates()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Audio States in Animator Controllers", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", EditorStyles.toolbarButton)) 
                AsAudioStateBackup.Instance.Export();
            if (GUILayout.Button("Import", EditorStyles.toolbarButton)) 
                AsAudioStateBackup.Instance.Import();
            if (GUILayout.Button("Compare", EditorStyles.toolbarButton)) 
                AsAudioStateBackup.Instance.Compare();
            if (GUILayout.Button("Remove All", EditorStyles.toolbarButton)) 
                AsAudioStateBackup.Instance.RemoveAll();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton)) 
                AsAudioStateBackup.Instance.OpenXmlFile();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTimeline()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Audio Playable Assets in Timeline", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.Export();
            if (GUILayout.Button("Import", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.Import();
            if (GUILayout.Button("Compare", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.Compare();
            if (GUILayout.Button("Remove All", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.RemoveAll();
            if (GUILayout.Button("Rename All", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.Rename();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton)) 
                AsTimelineAudioBackup.Instance.OpenXmlFile();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToggle(ref bool toggle, string label)
        {
            toggle = GUILayout.Toggle(toggle, label);
        }
    }        
}