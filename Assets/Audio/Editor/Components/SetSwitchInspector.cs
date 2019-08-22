using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(SetSwitch)), CanEditMultipleObjects]
    public class SetSwitchInspector : AsComponentInspector
    {
        private SetSwitch _component;

        private void OnEnable()
        {
            _component = target as SetSwitch;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowPhysicsSettings(_component, true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsGlobal"));

            AudioScriptGUI.DrawList(serializedObject.FindProperty("OnSwitches"), OnLabel(_component), AddOnSwitch);
            AudioScriptGUI.DrawList(serializedObject.FindProperty("OffSwitches"), OffLabel(_component), AddOffSwitch);

            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddOnSwitch(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.OnSwitches, new SetSwitchReference(evt.name));
            }
        }

        private void AddOffSwitch(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.OffSwitches, new SetSwitchReference(evt.name));
            }
        }
        
        protected override void Refresh()
        {
            foreach (var swc in _component.OnSwitches)
            {
                AsComponentBackup.RefreshSwitch(swc);
            }

            foreach (var swc in _component.OffSwitches)
            {
                AsComponentBackup.RefreshSwitch(swc);
            }
        }
    }
}