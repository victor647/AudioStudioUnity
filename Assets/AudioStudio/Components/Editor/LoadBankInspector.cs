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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AsyncMode"));
            if (_component.AsyncMode)
            {
                ShowSpatialSettings();
                ShowPhysicalSettings(_component, false);
                AsGuiDrawer.DrawList(serializedObject.FindProperty("Banks"), "SoundBanks:", AddBank);
            }
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Bank"));
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddBank(Object[] objects)
        {
            var banks = objects.Select(obj => obj as SoundBank).Where(a => a).ToArray();
            foreach (var evt in banks)
            {
                AsScriptingHelper.AddToArray(ref _component.Banks, new LoadBankReference(evt.name));
            }
        }

        protected override void Refresh()
        {
            foreach (var bank in _component.Banks)
            {
                AsComponentBackup.RefreshBank(bank);
            }
        }
    }
}