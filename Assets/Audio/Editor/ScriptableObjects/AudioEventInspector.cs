﻿using UnityEditor;
using UnityEngine;
using AudioStudio;

[CustomEditor(typeof(AudioEvent)), CanEditMultipleObjects]
public class AudioEventInspector : Editor
{
    protected static void RenameAsset(string newName, AudioEvent ae)
    {
        if (GUILayout.Button("Rename By Audio Clip"))
        {
            var assetPath =  AssetDatabase.GetAssetPath(ae.GetInstanceID());
            AssetDatabase.RenameAsset(assetPath, newName);	                 
        }			          
    }

    protected void DrawProperty(string fieldName, string labelName = "", int labelWidth = 116, int fieldWidth = 0)
    {
        if (labelName == "") labelName = fieldName;
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(labelName, GUILayout.Width(labelWidth));
        if (fieldWidth == 0)
            EditorGUILayout.PropertyField(serializedObject.FindProperty(fieldName), GUIContent.none);
        else
            EditorGUILayout.PropertyField(serializedObject.FindProperty(fieldName), GUIContent.none, GUILayout.Width(fieldWidth));
        GUILayout.EndHorizontal();
    }

    protected void DrawSubMixer(AudioEvent ae)
    {
        GUILayout.BeginHorizontal();  
        DrawProperty("SubMixer", "Sub Mixer", 116, 20);
        if (ae.SubMixer) 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioMixer"), GUIContent.none);
        GUILayout.EndHorizontal();
    }
    
    protected void DrawFilters(AudioEvent ae)
    {						
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LowPassFilter"));
        if (ae.LowPassFilter)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    Cutoff", GUILayout.Width(70));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LowPassCutoff"), GUIContent.none,GUILayout.Width(50));	
            EditorGUILayout.LabelField("Resonance", GUILayout.Width(70));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LowPassResonance"), GUIContent.none,GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }
			
        EditorGUILayout.PropertyField(serializedObject.FindProperty("HighPassFilter"));		
        if (ae.HighPassFilter)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    Cutoff", GUILayout.Width(70));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HighPassCutoff"), GUIContent.none,GUILayout.Width(50));	
            EditorGUILayout.LabelField("Resonance", GUILayout.Width(70));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HighPassResonance"), GUIContent.none,GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }
    }

    protected void DrawParameterMappings()
    {
        EditorGUILayout.LabelField("Map to AudioParameter(s)");
        AudioScriptGUI.DrawList(serializedObject.FindProperty("Mappings"));  
    }		    
}