using System.Linq;
using UnityEditor;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(EffectSound)), CanEditMultipleObjects]
public class EffectSoundInspector : AsComponentInspector
{
    private EffectSound _component;
    
    private void OnEnable()
    {
        _component = target as EffectSound;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        AudioScriptGUI.DrawList(serializedObject.FindProperty("EnableEvents"), "On Enable:", AddEnableEvent);

        serializedObject.ApplyModifiedProperties();
        ShowButtons(_component);      
    }   
    
    private void AddEnableEvent(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.EnableEvents, new AudioEventReference(evt.name));
        }								
    }
}