using UnityEditor;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(SimpleAudioPlayer)), CanEditMultipleObjects]
    public class SimpleAudioPlayerInspector : AsComponentInspector
    {
        private SimpleAudioPlayer _component;

        private void OnEnable()
        {
            _component = target as SimpleAudioPlayer;
            BackedUp = true;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowPhysicalSettings(_component, true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsGlobal"));

            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), OnLabel(_component));

            serializedObject.ApplyModifiedProperties();
            ShowButtons(_component);
        }
    }
}