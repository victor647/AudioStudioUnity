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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ResetSwitchesOnExit"));

            AsGuiDrawer.DrawList(serializedObject.FindProperty("EnterEvents"), "Enter Events", AddEnterEvent);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ExitEvents"), "Exit Events", AddExitEvent);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("EnterSwitches"), "Enter Switches", AddEnterSwitch);
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ExitSwitches"), "Exit Switches", AddExitSwitch);
            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        private void AddEnterEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.EnterEvents, new PostEventReference(evt));
            }
        }

        private void AddExitEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                AsScriptingHelper.AddToArray(ref _component.ExitEvents, new PostEventReference(evt));
            }
        }
        
        private void AddEnterSwitch(Object[] objects)
        {
            var switches = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();
            foreach (var swc in switches)
            {
                AsScriptingHelper.AddToArray(ref _component.EnterSwitches, new SetSwitchReference(swc.name, swc.DefaultSwitch));
            }
        }

        private void AddExitSwitch(Object[] objects)
        {
            var switches = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();
            foreach (var swc in switches)
            {
                AsScriptingHelper.AddToArray(ref _component.ExitSwitches, new SetSwitchReference(swc.name, swc.DefaultSwitch));
            }
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.EnterEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
            foreach (var evt in _component.ExitEvents)
            {
                AsComponentBackup.RefreshEvent(evt);
            }
            foreach (var swc in _component.EnterSwitches)
            {
                AsComponentBackup.RefreshSwitch(swc);
            }
            foreach (var swc in _component.ExitSwitches)
            {
                AsComponentBackup.RefreshSwitch(swc);
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