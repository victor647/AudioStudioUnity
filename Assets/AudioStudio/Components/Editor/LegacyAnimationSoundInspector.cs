using UnityEngine;
using UnityEditor;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(LegacyAnimationSound)), CanEditMultipleObjects]
    public class LegacyAnimationSoundInspector : AsComponentInspector
    {
        private LegacyAnimationSound _component;

        private void OnEnable()
        {
            _component = target as LegacyAnimationSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowSpatialSettings();
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("FrameRate"));
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events");
            serializedObject.ApplyModifiedProperties();
            CheckLinkedComponent();
            ShowButtons(_component);
        }

        private void CheckLinkedComponent()
        {
            var animator = _component.GetComponent<Animation>();
            if (animator.ToString() == "null")
                EditorGUILayout.HelpBox("Can't Find Legacy Animation Component!", MessageType.Error);
        }
    }
}
