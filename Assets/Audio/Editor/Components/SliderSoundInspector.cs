using UnityEditor;
using AudioStudio.Components;

using UnityEngine.UI;


namespace AudioStudio.Editor
{
    [CustomEditor(typeof(SliderSound)), CanEditMultipleObjects]
    public class SliderSoundInspector : AsComponentInspector
    {
        private SliderSound _component;

        private void OnEnable()
        {
            _component = target as SliderSound;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Connected Parameter:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ConnectedParameter"));
            EditorGUILayout.LabelField("On Drag:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DragEvent"));

            serializedObject.ApplyModifiedProperties();
            AudioScriptGUI.CheckLinkedComponent<Slider>(_component);
            ShowButtons(_component);
        }
    }
}