using System.Linq;
using UnityEngine;
using UnityEditor;
using AudioStudio;

[CustomEditor(typeof(AudioSwitch)), CanEditMultipleObjects]
public class AudioSwitchInspector : Editor 
{
    private AudioSwitch _audioSwitch;

    private void OnEnable()
    {
        _audioSwitch = target as AudioSwitch;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();		
        GUILayout.Label("Switch Names");
        AudioScriptGUI.DrawList(serializedObject.FindProperty("SwitchNames"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultSwitch"));
        AudioScriptGUI.DrawSaveButton(_audioSwitch);
        serializedObject.ApplyModifiedProperties();					
    }	        
}		

[CustomPropertyDrawer(typeof(SwitchEventMapping))]
public class SwitchAssignmentDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var fullWidth = position.width;
        
        EditorGUIUtility.labelWidth = 25;
        position.width = fullWidth * 0.4f;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("SwitchName"), new GUIContent("On"));		
		
        position.x += position.width + 3;
        EditorGUIUtility.labelWidth = 37;
        position.width = fullWidth * 0.6f;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("AudioEvent"), new GUIContent("plays"));
        
        EditorGUI.EndProperty();
    }
}
	
[CustomPropertyDrawer(typeof(SwitchClipMapping))]
public class VoiceSwitchAssignmentDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var fullWidth = position.width;
        
        EditorGUIUtility.labelWidth = 25;
        position.width = fullWidth * 0.4f;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("SwitchName"), new GUIContent("On"));		
		
        position.x += position.width + 3;
        EditorGUIUtility.labelWidth = 37;
        position.width = fullWidth * 0.6f;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("Clip"), new GUIContent("plays"));
        
        EditorGUI.EndProperty();
    }
}