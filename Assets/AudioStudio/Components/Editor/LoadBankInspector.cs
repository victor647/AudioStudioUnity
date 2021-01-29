using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(LoadBank)), CanEditMultipleObjects]
    public class LoadBankInspector : AsComponentInspector
    {
        private LoadBank _component;

        private void OnEnable()
        {
            _component = target as LoadBank;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowPhysicalSettings(_component, false);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AsyncMode"));
            if (_component.AsyncMode)
            {
                ShowSpatialSettings();
                ShowPhysicalSettings(_component, false);
                AsGuiDrawer.DrawList(serializedObject.FindProperty("AsyncBanks"), "SoundBanks:", AddAsyncBank);
            }
            else
                AsGuiDrawer.DrawList(serializedObject.FindProperty("SyncBanks"), "SoundBanks:", AddSyncBank);
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddAsyncBank(Object[] objects)
        {
            var banks = objects.Select(obj => obj as SoundBank).Where(a => a).ToArray();
            foreach (var bank in banks)
            {
                AsScriptingHelper.AddToArray(ref _component.AsyncBanks, new LoadBankReference(bank.name));
            }
        }
        
        private void AddSyncBank(Object[] objects)
        {
            var banks = objects.Select(obj => obj as SoundBank).Where(a => a).ToArray();
            foreach (var bank in banks)
            {
                AsScriptingHelper.AddToArray(ref _component.SyncBanks, bank);
            }
        }

        protected override void Refresh()
        {
            foreach (var bank in _component.AsyncBanks)
            {
                AsComponentBackup.RefreshBank(bank);
            }
        }
    }
}