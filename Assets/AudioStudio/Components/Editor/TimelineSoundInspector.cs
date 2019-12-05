using UnityEditor;
using AudioStudio.Components;
using UnityEngine.Playables;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(TimelineSound)), CanEditMultipleObjects]
    public class TimelineSoundInspector : AsComponentInspector
    {
        private TimelineSound _component;

        private void OnEnable()
        {
            _component = target as TimelineSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowSpatialSettings();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("Emitters"), "Emitters");
            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<PlayableDirector>(_component);
            ShowButtons(_component);
        }
    }

}