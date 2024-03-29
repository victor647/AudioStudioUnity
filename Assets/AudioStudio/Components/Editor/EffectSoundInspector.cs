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
            ShowSpatialSettings();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("EnableEvents"), "On Enable:", AddEnableEvent);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("DisableEvents"), "On Disable:", AddEnableEvent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DelayTime"));
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddEnableEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.EnableEvents, new PostEventReference(evt));
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