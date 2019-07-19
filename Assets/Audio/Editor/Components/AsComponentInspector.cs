using AudioStudio;
using UnityEditor;
using UnityEngine;

public abstract class AsComponentInspector : Editor
{			
	protected string OnLabel(AudioPhysicsHandler aph)
	{
		switch (aph.SetOn)
		{
			case TriggerCondition.EnableDisable:
				return "On Enable:";			
			case TriggerCondition.TriggerEnterExit:
				return "On Trigger Enter:";
			case TriggerCondition.CollisionEnterExit:
				return "On Collision Enter:";
		}
		return string.Empty;
	}
	
	protected string OffLabel(AudioPhysicsHandler aph)
	{
		switch (aph.SetOn)
		{
			case TriggerCondition.EnableDisable:
				return "On Disable:";			
			case TriggerCondition.TriggerEnterExit:
				return "On Trigger Exit:";
			case TriggerCondition.CollisionEnterExit:
				return "On Collision Exit:";
		}
		return string.Empty;
	}

	protected void ShowPhysicsSettings(AudioPhysicsHandler aph, bool is3D)
	{
		EditorGUILayout.LabelField("Trigger Settings", EditorStyles.boldLabel);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box))
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("SetOn"));
			if (aph.SetOn != TriggerCondition.EnableDisable)
			{
				if (is3D) EditorGUILayout.PropertyField(serializedObject.FindProperty("PostFrom"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioTag"));
			}
		}
	}

	protected void ShowButtons(Object component)
	{		
		EditorGUILayout.Separator();
		EditorGUILayout.BeginHorizontal();
		GUI.contentColor = Color.yellow;		
		if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
		{
			Refresh();
		}		
		GUI.contentColor = Color.green;
		if (GUILayout.Button("Save", EditorStyles.toolbarButton))
		{   						
			UpdateXml(component, XmlAction.Save);			
		}
		GUI.contentColor = Color.magenta;
		if (GUILayout.Button("Revert", EditorStyles.toolbarButton))
		{                			            			 
			UpdateXml(component, XmlAction.Revert);
		}
		GUI.contentColor = Color.red;
		if (GUILayout.Button("Remove", EditorStyles.toolbarButton))
		{						 
			UpdateXml(component, XmlAction.Remove);		
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();
	}	
	
	protected virtual void Refresh(){}

	protected virtual void UpdateXml(Object component, XmlAction action)
	{
		var c = (AsComponent) component;				
		switch (action)
		{
			case XmlAction.Remove:				
				AsComponentBackup.Instance.RemoveComponentNode(c);				
				DestroyImmediate(component, true);
				break;
			case XmlAction.Save:				
				AsComponentBackup.Instance.UpdateComponentNode(c);
				break;
			case XmlAction.Revert:
				AsComponentBackup.Instance.RevertComponentToXml(c);
				break;
		}
		AssetDatabase.SaveAssets();
	}
}
