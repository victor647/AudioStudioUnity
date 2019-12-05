using System.Linq;
using UnityEngine;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioState)), CanEditMultipleObjects]
    public class AudioStateInspector : AsComponentInspector
    {
        private AudioState _component;

        private void OnEnable()
        {
            _component = target as AudioState;
            CheckXmlExistence();
        }

        private void CheckXmlExistence()
        {
            var path = AssetDatabase.GetAssetPath(_component);
            var state = "OnLayer";
            var layer = AsAudioStateBackup.GetLayerStateName(_component, ref state);
            BackedUp = AsAudioStateBackup.Instance.ComponentBackedUp(path, layer, state);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Set Audio State:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AnimationAudioState"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ResetStateOnExit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("StopEventsOnExit"));

            AsGuiDrawer.DrawList(serializedObject.FindProperty("EnterEvents"), "On State Enter:", AddEnterEvent);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ExitEvents"), "On State Enter:", AddExitEvent);

            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddEnterEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.EnterEvents, new AudioEventReference(evt.name));
            }
        }

        private void AddExitEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.ExitEvents, new AudioEventReference(evt.name));
            }
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.EnterEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
        }

        protected override void UpdateXml(Object obj, XmlAction action)
        {
            var edited = false;
            var component = (AudioState) obj;
            var path = AssetDatabase.GetAssetPath(component);
            
            switch (action)
            {
                case XmlAction.Remove:
                    AsAudioStateBackup.Instance.RemoveComponentXml(path, component);
                    break;
                case XmlAction.Save:
                    edited = AsAudioStateBackup.Instance.UpdateXmlFromComponent(path, component);
                    break;
                case XmlAction.Revert:
                    edited = AsAudioStateBackup.Instance.RevertComponentToXml(path, component);
                    break;
            }
            BackedUp = true;
            if (edited) 
                AssetDatabase.SaveAssets();
        }
    }
}