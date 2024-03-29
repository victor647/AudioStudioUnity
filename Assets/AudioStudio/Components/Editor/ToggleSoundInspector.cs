﻿using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(ToggleSound)), CanEditMultipleObjects]
    public class ToggleSoundInspector : AsComponentInspector
    {
        private ToggleSound _component;

        private void OnEnable()
        {
            _component = target as ToggleSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ToggleOnEvents"), "Toggle On:", AddOnEvent);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ToggleOffEvents"), "Toggle Off:", AddOffEvent);

            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<Toggle>(_component);
            ShowButtons(_component);
        }

        private void AddOnEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.ToggleOnEvents, new PostEventReference(evt));
            }
        }

        private void AddOffEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.ToggleOffEvents, new PostEventReference(evt));
            }
        }
        
        protected override void Refresh()
        {
            foreach (var evt in _component.ToggleOnEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }

            foreach (var evt in _component.ToggleOffEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
        }
    }
}