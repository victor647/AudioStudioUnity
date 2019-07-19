using System.Collections;
using System.Collections.Generic;
using AudioStudio;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioTag)), CanEditMultipleObjects]
public class AudioTagInspector : AsComponentInspector {

	private AudioTag _component;

	private void OnEnable()
	{
		_component = target as AudioTag;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();		
		EditorGUILayout.PropertyField(serializedObject.FindProperty("Tags"));        		    
		serializedObject.ApplyModifiedProperties();                		
		ShowButtons(_component);
	}
}
