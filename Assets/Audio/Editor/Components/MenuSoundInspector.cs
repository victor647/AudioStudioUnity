using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(MenuSound)), CanEditMultipleObjects]
    public class MenuSoundInspector : AsComponentInspector
    {
        private MenuSound _component;

        private void OnEnable()
        {
            _component = target as MenuSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AudioScriptGUI.DrawList(serializedObject.FindProperty("OpenEvents"), "On Menu Open:", AddOpenEvent);
            AudioScriptGUI.DrawList(serializedObject.FindProperty("CloseEvents"), "On Close Open:", AddCloseEvent);

            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddOpenEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.OpenEvents, new AudioEventReference(evt.name));
            }
        }

        private void AddCloseEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AudioUtility.AddToArray(ref _component.CloseEvents, new AudioEventReference(evt.name));
            }
        }
        
        protected override void Refresh()
        {
            foreach (var evt in _component.OpenEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }

            foreach (var evt in _component.CloseEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
        }
    }
}