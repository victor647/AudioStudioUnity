using AudioStudio.Configs;
using AudioStudio.Midi;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(MusicInstrument)), CanEditMultipleObjects]
    public class MusicInstrumentInspector : AudioEventInspector
    {
        private MusicInstrument _instrument;

        private void OnEnable()
        {
            _instrument = target as MusicInstrument;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SampleType"));
            if (_instrument.SampleType != InstrumentSampleType.OneShotTrigger)
            {
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MaxPolyphonicVoices"));
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Attack"));
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Release"));
            }
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("VelocityCurve"));
            AsGuiDrawer.DrawList(serializedObject.FindProperty("KeyboardMappings"), "Keyboard Mappings");
            AsGuiDrawer.DrawSaveButton(_instrument);
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    [CustomPropertyDrawer(typeof(KeyboardMapping))]
    public class KeyboardMappingDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var multiNote = property.FindPropertyRelative("MultiNote");
            var lowestNote = property.FindPropertyRelative("LowestNote");
            var highestNote = property.FindPropertyRelative("HighestNote");
            var centerNote = property.FindPropertyRelative("CenterNote");
            EditorGUI.PropertyField(position, multiNote);

            EditorGUILayout.EndHorizontal();
            
            if (multiNote.boolValue)
            {
                if (lowestNote.intValue > highestNote.intValue)
                    lowestNote.intValue = highestNote.intValue;
                if (centerNote.intValue > highestNote.intValue)
                    centerNote.intValue = highestNote.intValue;
                if (centerNote.intValue < lowestNote.intValue)
                    centerNote.intValue = lowestNote.intValue;
                
                EditorGUILayout.LabelField("Note Number Range");
                DrawNoteField(lowestNote, "Lowest");
                DrawNoteField(highestNote, "Highest");
                DrawNoteField(centerNote, "Center");
            }
            else
                DrawNoteField(centerNote, "Note");

            EditorGUILayout.LabelField("Samples (Random)");
            AsGuiDrawer.DrawList(property.FindPropertyRelative("Samples"));
            
            EditorGUILayout.BeginHorizontal();
        }

        private void DrawNoteField(SerializedProperty property, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MaxWidth(60));
            EditorGUILayout.PropertyField(property, GUIContent.none);
            EditorGUILayout.LabelField(MidiConsole.GetNoteName((byte) property.intValue), EditorStyles.whiteLabel, GUILayout.MaxWidth(40));
            EditorGUILayout.EndHorizontal();
        }
    }
}