using UnityEngine;
using UnityEditor;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(AnimationSound)), CanEditMultipleObjects]
    public class AnimationSoundInspector : AsComponentInspector
    {
        private AnimationSound _component;

        private void OnEnable()
        {
            _component = target as AnimationSound;
            CheckXmlExistence(_component);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ShowSpatialSettings();
            serializedObject.ApplyModifiedProperties();
            AsGuiDrawer.CheckLinkedComponent<Animator>(_component);
            ShowButtons(_component);
        }
    }
}
