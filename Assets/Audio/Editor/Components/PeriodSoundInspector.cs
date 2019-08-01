using UnityEngine;
using UnityEditor;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(PeriodSound)), CanEditMultipleObjects]
	public class PeriodSoundInspector : AsComponentInspector
	{
		private PeriodSound _component;

		private void OnEnable()
		{
			_component = target as PeriodSound;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("InitialDelay"));
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Trigger Interval", GUILayout.Width(116));
			EditorGUILayout.LabelField("Min", GUILayout.Width(30));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("MinInterval"), GUIContent.none, GUILayout.Width(30));
			EditorGUILayout.LabelField("Max", GUILayout.Width(30));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxInterval"), GUIContent.none, GUILayout.Width(30));
			GUILayout.EndHorizontal();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioEvent"));
			serializedObject.ApplyModifiedProperties();
			ShowButtons(_component);
		}
	}
}