using UnityEngine;
using UnityEditor;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
    [CustomPropertyDrawer(typeof(AnimationAudioEvent))]
    public class AnimationAudioEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var totalWidth = position.width;
		
            position.width = 40;
            EditorGUI.LabelField(position, "Frame");
            position.x += 42;
        
            position.width = 30;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("Frame"), GUIContent.none);
            position.x += 32;
        
            position.width = 30;
            EditorGUI.LabelField(position, "Clip");
            position.x += 32;
        
            position.width = totalWidth - 105;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("ClipName"), GUIContent.none);
        
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property.FindPropertyRelative("AudioEvent"), GUIContent.none);
        }
    }
    
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
