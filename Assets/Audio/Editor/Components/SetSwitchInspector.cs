using System.Linq;
using UnityEditor;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(SetSwitch)), CanEditMultipleObjects]
public class SetSwitchInspector : AsComponentInspector
{    
    private SetSwitch _component;

    private void OnEnable()
    {
        _component = target as SetSwitch;
    }
    
    public override void OnInspectorGUI()
    {        
        serializedObject.Update();                
        ShowPhysicsSettings(_component, true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("IsGlobal"));
        
        AudioScriptGUI.DrawList(serializedObject.FindProperty("OnSwitches"), OnLabel(_component), AddOnSwitch);
        AudioScriptGUI.DrawList(serializedObject.FindProperty("OffSwitches"), OffLabel(_component), AddOffSwitch);
                
        serializedObject.ApplyModifiedProperties();
        ShowButtons(_component);
    }        
    
    private void AddOnSwitch(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.OnSwitches, new SetSwitchReference(evt.name));
        }								
    }
    
    private void AddOffSwitch(Object[] objects)
    {
        var events = objects.Select(obj => obj as AudioSwitch).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.OffSwitches, new SetSwitchReference(evt.name));
        }								
    }
}