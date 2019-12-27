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
            CheckLinkedComponent();
            ShowButtons(_component);
        }

        private void CheckLinkedComponent()
        {
            var animator = _component.GetComponent<Animator>();
            if (animator.ToString() == "null")
            {
                var animation = _component.GetComponent<Animation>();
                if (animation == null)
                    EditorGUILayout.HelpBox("Can't Find Animator or Animation Component!", MessageType.Error);
            }
        }
    }
}
