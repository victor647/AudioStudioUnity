using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Tools;
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
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Connected Parameter:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ConnectedParameter"));
            EditorGUILayout.LabelField("On Drag:");
            AsGuiDrawer.DrawAudioObject(serializedObject, "DragEvent");

            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<Slider>(_component);
            ShowButtons(_component);
        }
        
        protected override void Refresh()
        {
            AsComponentBackup.RefreshEvent(_component.DragEvent);
            AsComponentBackup.RefreshParameter(_component.ConnectedParameter);
        }
    }
}