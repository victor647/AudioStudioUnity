using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
	public abstract class AsComponentInspector : UnityEditor.Editor
	{
		protected bool BackedUp;
		
		protected static string OnLabel(AudioPhysicsHandler aph)
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

		protected static string OffLabel(AudioPhysicsHandler aph)
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
		
		protected void CheckXmlExistence(AsComponent component)
		{
			BackedUp = AsComponentBackup.Instance.FindComponentNode(component) != null;
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

		protected virtual void Refresh()
		{
		}

		protected virtual void UpdateXml(Object component, XmlAction action)
		{
			var c = (AsComponent) component;
			var go = c.gameObject;
			var edited = false;
			switch (action)
			{
				case XmlAction.Remove:
					AsComponentBackup.Instance.RemoveComponentNode(c);
					edited = true;
					break;
				case XmlAction.Save:
					edited = AsComponentBackup.Instance.UpdateComponentNode(c);
					break;
				case XmlAction.Revert:
					edited = AsComponentBackup.Instance.RevertComponentToXml(c);
					break;
			}
			BackedUp = true;
			if (edited)
				AsComponentBackup.SaveComponentAsset(go);
		}
	}
}