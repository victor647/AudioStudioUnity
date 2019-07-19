
using System.Linq;
using UnityEditor;
using AudioStudio;
using UnityEngine;

[CustomEditor(typeof(ColliderSound)), CanEditMultipleObjects]
public class ColliderSoundInspector : AsComponentInspector
{
	private ColliderSound _component;	

	private void OnEnable()
	{
		_component = target as ColliderSound;
	}
	
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		
								
		EditorGUILayout.PropertyField(serializedObject.FindProperty("PostFrom"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioTag"));
		AudioScriptGUI.DrawList(serializedObject.FindProperty("EnterEvents"), "On Collision/Trigger Enter:", AddEnterEvent);
		AudioScriptGUI.DrawList(serializedObject.FindProperty("ExitEvents"), "On Collision/Trigger Exit:", AddExitEvent);		
        	
		EditorGUILayout.PropertyField(serializedObject.FindProperty("CollisionForceParameter"));
				
		serializedObject.ApplyModifiedProperties();
		AudioScriptGUI.CheckLinkedComponent<Collider>(_component);
		ShowButtons(_component); 
	} 
	
	private void AddEnterEvent(Object[] objects)
	{
		var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
		foreach (var evt in events)
		{
			AudioUtility.AddToArray(ref _component.EnterEvents, new AudioEventReference(evt.name));
		}								
	}
	
	private void AddExitEvent(Object[] objects)
	{
		var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();                   
		foreach (var evt in events)
		{
			AudioUtility.AddToArray(ref _component.ExitEvents, new AudioEventReference(evt.name));
		}								
	}
}
