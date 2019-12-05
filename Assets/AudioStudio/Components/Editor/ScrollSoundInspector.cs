using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine.UI;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(ScrollSound)), CanEditMultipleObjects]
    public class ScrollSoundInspector : AsComponentInspector
    {
        private ScrollSound _component;

        private void OnEnable()
        {
            _component = target as ScrollSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("On Scroll:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ScrollEvent"));

            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<ScrollRect>(_component);
            ShowButtons(_component);
        }
        
        protected override void Refresh()
        {
            AsComponentBackup.RefreshEvent(_component.ScrollEvent);
        }
    }
}