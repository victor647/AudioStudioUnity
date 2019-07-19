using System.Linq;
using UnityEditor;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(LoadBank)), CanEditMultipleObjects]
public class LoadBankInspector : AsComponentInspector
{
    private LoadBank _component;

    private void OnEnable()
    {
        _component = target as LoadBank;
    }
    
    public override void OnInspectorGUI()
    {        
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UnloadOnDisable"));
        AudioScriptGUI.DrawList(serializedObject.FindProperty("Banks"), "Banks:", AddBank);
        serializedObject.ApplyModifiedProperties();
        ShowButtons(_component); 
    }
    
    private void AddBank(Object[] objects)
    {
        var events = objects.Select(obj => obj as SoundBank).Where(a => a).ToArray();                   
        foreach (var evt in events)
        {
            AudioUtility.AddToArray(ref _component.Banks, new SoundBankReference(evt.name));
        }								
    }
}