using UnityEditor;
using AudioStudio;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ScrollSound)), CanEditMultipleObjects]
public class ScrollSoundInspector : AsComponentInspector
{
    private ScrollSound _component;

    private void OnEnable()
    {
        _component = target as ScrollSound;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("On Scroll:");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ScrollEvent"));
        
        serializedObject.ApplyModifiedProperties();
        AudioScriptGUI.CheckLinkedComponent<ScrollRect>(_component);
        ShowButtons(_component);
    }
}