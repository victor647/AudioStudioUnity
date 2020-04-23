using AudioStudio.Components;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(AudioListener3D)), CanEditMultipleObjects]
	public class AudioListener3DInspector : AsComponentInspector
	{

		private AudioListener3D _component;

		private void OnEnable()
		{
			_component = target as AudioListener3D;
			CheckXmlExistence(_component);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("PositionOffset"));
			AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MoveZAxisByCameraFOV"), "", 200);
			if (_component.MoveZAxisByCameraFOV)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("  Field of View", GUILayout.Width(100));
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MinFOV"), "Min", 30, 40);
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MaxFOV"), "Max", 30, 40);
				EditorGUILayout.EndHorizontal();
				
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("  Z Axis Offset", GUILayout.Width(100));
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MinOffset"), "Min", 30, 40);
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("MaxOffset"), "Max", 30, 40);
				EditorGUILayout.EndHorizontal();
			}
			serializedObject.ApplyModifiedProperties();
			ShowButtons(_component);
		}
	}
}