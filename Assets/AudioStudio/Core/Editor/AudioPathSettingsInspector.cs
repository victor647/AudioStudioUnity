using System;
using System.Diagnostics;
using System.IO;
using AudioStudio.Tools;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioPathSettings))]
    public class AudioPathSettingsInspector : UnityEditor.Editor
    {
        private AudioPathSettings _component;

        private void OnEnable()
        {
            _component = target as AudioPathSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawPathDisplay("AudioStudio Library Path", AudioPathSettings.AudioStudioLibraryPath, SetupAudioStudioLibrary);
                DrawPathDisplay("Original Resource Path", _component.OriginalResourcesPath, SetupOriginalResourcesPath);
                DrawPathDisplay("Build Assets Path", _component.BuildAssetsPath, SetupBuildAssetsPath);
                DrawPathDisplay("Game Start Scene Path", _component.StartScenePath, SetupStartScenePath);
            }

            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MusicQuality"));
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SoundQuality"));
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("VoiceQuality"));
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("StreamDurationThreshold"), "", 165);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.yellow;
            if (GUILayout.Button("Open Configs Folder", EditorStyles.toolbarButton))
                Process.Start(AudioPathSettings.EditorConfigPathFull);
            AsGuiDrawer.DrawSaveButton(_component);
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPathDisplay(string title, string path, Action action)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (GUILayout.Button("Locate", GUILayout.Width(50))) action();
            EditorGUILayout.EndHorizontal();
            if (string.IsNullOrEmpty(path))
                EditorGUILayout.HelpBox("Path not set!", MessageType.Error);
            else
                EditorGUILayout.LabelField(path);
            EditorGUILayout.Separator();
        }

        private void SetupAudioStudioLibrary()
        {
            var scriptPath = AsScriptingHelper.CombinePath(Application.dataPath, AudioPathSettings.AudioStudioLibraryPath, "Extensions/AsPathSettingsExt.cs");
            Process.Start(scriptPath);
        }


        private void SetupOriginalResourcesPath()
        {
            var oldPath = AsScriptingHelper.CombinePath(Application.dataPath, _component.OriginalResourcesPath);
            var pathNew = EditorUtility.OpenFolderPanel("Select audio original resources folder", oldPath, "");
            if (!string.IsNullOrEmpty(pathNew))
                _component.OriginalResourcesPath = AsScriptingHelper.ShortPath(pathNew, false);
        }

        private void SetupBuildAssetsPath()
        {
            var oldPath = AsScriptingHelper.CombinePath(Application.dataPath, _component.BuildAssetsPath);
            var pathNew = EditorUtility.OpenFolderPanel("Select audio build assets folder", oldPath, "");
            if (!string.IsNullOrEmpty(pathNew))
                _component.BuildAssetsPath = AsScriptingHelper.ShortPath(pathNew, false);
        }

        private void SetupStartScenePath()
        {
            var oldPath = AsScriptingHelper.CombinePath(Application.dataPath, _component.StartScenePath);
            var defaultDirectory = Path.GetDirectoryName(oldPath);
            var pathNew = EditorUtility.OpenFilePanel("Select scene to start game", defaultDirectory, "unity");
            if (!string.IsNullOrEmpty(pathNew))
                _component.StartScenePath = AsScriptingHelper.ShortPath(pathNew, false);
        }
    }
}