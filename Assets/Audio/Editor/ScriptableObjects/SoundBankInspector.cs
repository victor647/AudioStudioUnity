using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AudioStudio;
using Object = UnityEngine.Object;

[CustomEditor(typeof(SoundBank)), CanEditMultipleObjects]
public class SoundBankInspector : Editor {

    private SoundBank _soundBank;

    private void OnEnable()
    {
        _soundBank = target as SoundBank;
    }
	
    public override void OnInspectorGUI()
    {		
        serializedObject.Update();
        
        DrawEvents();
        DrawControllers();
        AudioScriptGUI.DrawSaveButton(_soundBank);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEvents()
    {
        AudioScriptGUI.DrawList(serializedObject.FindProperty("AudioEvents"), "Sound Events:", AddEvent);
        GUI.contentColor = Color.red;
        if (_soundBank.AudioEvents.Count > 0 && GUILayout.Button("Clear All Events", EditorStyles.miniButton))        
            _soundBank.AudioEvents.Clear(); 
        GUI.contentColor = Color.white;
        EditorGUILayout.Separator();
    }
    
    private void DrawControllers()
    {
        AudioScriptGUI.DrawList(serializedObject.FindProperty("AudioControllers"), "Audio Controllers:", AddController);
        GUI.contentColor = Color.red;
        if (_soundBank.AudioControllers.Count > 0 && GUILayout.Button("Clear All Controllers", EditorStyles.miniButton))        
            _soundBank.AudioControllers.Clear();
        GUI.contentColor = Color.white;
        EditorGUILayout.Separator();
    }

    private void ClearAll(Action action)
    {
        
    }
    
    private void AddEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as SoundContainer).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            _soundBank.RegisterEvent(evt);	
        }								
    }
    
    private void AddController(Object[] objects)
    {
        var controllers = objects.Select(obj => obj as AudioController).Where(a => a).ToArray();           
        foreach (var controller in controllers)
        {
            _soundBank.RegisterController(controller);	
        }								
    }
}