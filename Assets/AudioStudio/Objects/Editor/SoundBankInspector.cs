using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AudioStudio.Configs;
using AudioStudio.Tools;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(SoundBank)), CanEditMultipleObjects]
    public class SoundBankInspector : UnityEditor.Editor
    {

        private SoundBank _soundBank;

        private void OnEnable()
        {
            _soundBank = target as SoundBank;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseLoadCounter"));
            DrawEvents();
            DrawControllers();
            AsGuiDrawer.DrawPathDisplay("Audio Events Folder", _soundBank.EventsFolder, SetupEventsFolderPath);
            
            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.yellow;
            if (GUILayout.Button("Refresh & Sort", EditorStyles.toolbarButton))
            {
                AsBatchProcessor.RefreshBankFolder(_soundBank);
                _soundBank.Sort();
            }
            AsGuiDrawer.DrawSaveButton(_soundBank);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEvents()
        {
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events:", AddEvent);
            GUI.contentColor = Color.red;
            if (_soundBank.AudioEvents.Count > 0 && GUILayout.Button("Clear All Events", EditorStyles.miniButton))
                _soundBank.AudioEvents.Clear();
            GUI.contentColor = Color.white;
            EditorGUILayout.Separator();
        }

        private void DrawControllers()
        {
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioControllers"), "Audio Controllers:", AddController);
            GUI.contentColor = Color.red;
            if (_soundBank.AudioControllers.Count > 0 && GUILayout.Button("Clear All Controllers", EditorStyles.miniButton))
                _soundBank.AudioControllers.Clear();
            GUI.contentColor = Color.white;
            EditorGUILayout.Separator();
        }

        private void AddEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                if (evt.IndependentEvent)
                    _soundBank.RegisterEvent(evt);
            }
        }

        private void AddController(Object[] objects)
        {
            var controllers = objects.Select(obj => obj as AudioController).Where(a => a).ToArray();
            foreach (var controller in controllers)
            {
                _soundBank.RegisterController(controller);
            }
        }
        
        private void SetupEventsFolderPath()
        {
            var oldPath = string.IsNullOrEmpty(_soundBank.EventsFolder) ? "Assets/" + AudioPathSettings.Instance.SoundEventsPath : AsScriptingHelper.CombinePath(Application.dataPath, _soundBank.EventsFolder);
            var pathNew = EditorUtility.OpenFolderPanel("Select audio events folder", oldPath, "");
            if (!string.IsNullOrEmpty(pathNew))
                _soundBank.EventsFolder = AsScriptingHelper.ShortPath(pathNew, false);
        }
    }
}