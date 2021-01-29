using UnityEditor;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AudioInitLoadData))]
    public class AudioInitLoadDataInspector : UnityEditor.Editor
    {
        private AudioInitLoadData _component;

        private void OnEnable()
        {
            _component = target as AudioInitLoadData;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("LoadBanks"));
            if (_component.LoadBanks)
                AsGuiDrawer.DrawList(serializedObject.FindProperty("Banks"), "Global Banks");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PostEvents"));
            if (_component.PostEvents)
                AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"));

            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.DrawSaveButton(_component);
        }
    }
}