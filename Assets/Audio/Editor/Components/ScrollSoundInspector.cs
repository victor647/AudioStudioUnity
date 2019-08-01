using UnityEditor;
using AudioStudio.Components;
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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("On Scroll:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ScrollEvent"));

            serializedObject.ApplyModifiedProperties();
            AudioScriptGUI.CheckLinkedComponent<ScrollRect>(_component);
            ShowButtons(_component);
        }
    }
}