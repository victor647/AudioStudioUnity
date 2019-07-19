
using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(ToggleSound)), CanEditMultipleObjects]
public class ToggleSoundInspector : AsComponentInspector
{
    private ToggleSound _component;

    private void OnEnable()
    {
        _component = target as ToggleSound;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        AudioScriptGUI.DrawList(serializedObject.FindProperty("ToggleOnEvents"), "Toggle On:", AddOnEvent);
        AudioScriptGUI.DrawList(serializedObject.FindProperty("ToggleOffEvents"), "Toggle Off:", AddOffEvent);
                
        serializedObject.ApplyModifiedProperties();
        AudioScriptGUI.CheckLinkedComponent<Toggle>(_component);
        ShowButtons(_component);
    }     
    
    private void AddOnEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.ToggleOnEvents, new AudioEventReference(evt.name));
        }								
    }
    
    private void AddOffEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.ToggleOffEvents, new AudioEventReference(evt.name));
        }								
    }
}