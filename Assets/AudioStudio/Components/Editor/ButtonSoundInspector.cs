using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(ButtonSound)), CanEditMultipleObjects]
    public class ButtonSoundInspector : AsComponentInspector
    {
        private ButtonSound _component;

        private void OnEnable()
        {
            _component = target as ButtonSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ClickEvents"), "On Click:", AddClickEvent);
            EditorGUILayout.LabelField("Mouse Enter:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PointerEnterEvent"));
            EditorGUILayout.LabelField("Mouse Exit:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PointerExitEvent"));

            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<Button>(_component);
            ShowButtons(_component);
        }

        private void AddClickEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.ClickEvents, new AudioEventReference(evt.name));
            }
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.ClickEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
            AsComponentBackup.RefreshEvent(_component.PointerEnterEvent);
            AsComponentBackup.RefreshEvent(_component.PointerExitEvent);
        }
    }
}