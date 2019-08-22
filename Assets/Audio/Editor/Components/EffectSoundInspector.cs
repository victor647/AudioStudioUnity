using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(EffectSound)), CanEditMultipleObjects]
    public class EffectSoundInspector : AsComponentInspector
    {
        private EffectSound _component;

        private void OnEnable()
        {
            _component = target as EffectSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AudioScriptGUI.DrawList(serializedObject.FindProperty("EnableEvents"), "On Enable:", AddEnableEvent);

            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddEnableEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.EnableEvents, new AudioEventReference(evt.name));
            }
        }
        
        protected override void Refresh()
        {
            foreach (var evt in _component.EnableEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
        }
    }
}