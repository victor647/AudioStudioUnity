using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Tools;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(EventSound)), CanEditMultipleObjects]
    public class EventSoundInspector : AsComponentInspector
    {
        private EventSound _component;

        private void OnEnable()
        {
            _component = target as EventSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events");
            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }

        protected override void Refresh()
        {
            foreach (var evt in _component.AudioEvents)
            {
                AsComponentBackup.RefreshEvent(evt.AudioEvent);   
            }
        }
    }
}