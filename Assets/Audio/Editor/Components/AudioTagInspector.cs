using AudioStudio.Components;
using UnityEditor;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(AudioTag)), CanEditMultipleObjects]
	public class AudioTagInspector : AsComponentInspector
	{

		private AudioTag _component;

		private void OnEnable()
		{
			_component = target as AudioTag;
			CheckXmlExistence(_component);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Tags"));
			serializedObject.ApplyModifiedProperties();
			ShowButtons(_component);
		}
	}
}