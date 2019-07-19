﻿using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(DropdownSound)), CanEditMultipleObjects]
public class DropdownSoundInspector : AsComponentInspector
{
    private DropdownSound _component;

    private void OnEnable()
    {
        _component = target as DropdownSound;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        AudioScriptGUI.DrawList(serializedObject.FindProperty("ValueChangeEvents"), "On Select:", AddValueChangeEvent);
        AudioScriptGUI.DrawList(serializedObject.FindProperty("PopupEvents"), "On Expand:", AddPopupEvent);
        AudioScriptGUI.DrawList(serializedObject.FindProperty("CloseEvents"), "On Fold:", AddCloseEvent);
                
        serializedObject.ApplyModifiedProperties();
        AudioScriptGUI.CheckLinkedComponent<Dropdown>(_component);
        ShowButtons(_component);  
    }     
    
    private void AddValueChangeEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.ValueChangeEvents, new AudioEventReference(evt.name));
        }								
    }
	
    private void AddPopupEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.PopupEvents, new AudioEventReference(evt.name));
        }								
    }
    
    private void AddCloseEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.CloseEvents, new AudioEventReference(evt.name));
        }								
    }
}