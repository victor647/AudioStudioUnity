using System.Linq;
using UnityEditor;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioInitSettings))]
    public class AudioInitSettingsInspector : UnityEditor.Editor
    {
        private AudioInitSettings _component;

        private void OnEnable()
        {
            _component = target as AudioInitSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioMixer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseMicrophone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseMidi"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LoadBanks"));
            if (_component.LoadBanks)
                AsGuiDrawer.DrawList(serializedObject.FindProperty("StartBanks"), "", AddBank);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PostEvents"));
            if (_component.PostEvents)
                AsGuiDrawer.DrawList(serializedObject.FindProperty("StartEvents"), "", AddEvent);
            AsGuiDrawer.DrawSaveButton(_component);
            serializedObject.ApplyModifiedProperties();
        }

        private void AddEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.StartEvents, new AudioEventReference(evt.name));
            }
        }

        private void AddBank(Object[] objects)
        {
            var banks = objects.Select(obj => obj as SoundBank).Where(a => a).ToArray();
            foreach (var bank in banks)
            {
                AsScriptingHelper.AddToArray(ref _component.StartBanks, new SoundBankReference(bank.name));
            }
        }
    }
}