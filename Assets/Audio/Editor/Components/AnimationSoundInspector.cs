using UnityEngine;
using UnityEditor;
using AudioStudio;

[CustomEditor(typeof(AnimationSound)), CanEditMultipleObjects]
public class AnimationSoundInspector : AsComponentInspector
{
    private AnimationSound _component;

    private void OnEnable()
    {
        _component = target as AnimationSound;
    }

    public override void OnInspectorGUI()
    {           
        AudioScriptGUI.CheckLinkedComponent<Animator>(_component);
        ShowButtons(_component);    
    }
}

