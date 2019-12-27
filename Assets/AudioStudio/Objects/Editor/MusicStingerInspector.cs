using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(MusicStinger)), CanEditMultipleObjects]
	public class MusicStingerInspector : MusicContainerInspector
	{
		private MusicStinger _musicStinger;

		private void OnEnable()
		{
			_musicStinger = target as MusicStinger;
		}

		public override void OnInspectorGUI()
		{		
			serializedObject.Update();
			DrawAudioClipData();
			AsGuiDrawer.DrawProperty(serializedObject.FindProperty("TriggerSync"));
			EditorGUILayout.Separator();
			DrawAudioControls(_musicStinger);
			serializedObject.ApplyModifiedProperties();
			DrawAuditionButtons(_musicStinger);
			AsGuiDrawer.DrawSaveButton(_musicStinger);
		}
		

		private void DrawAudioClipData()
		{
			EditorGUILayout.LabelField("Audio Data", EditorStyles.boldLabel);   
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				EditorGUILayout.LabelField("Clip Key Assignments");
				AsGuiDrawer.DrawList(serializedObject.FindProperty("KeyAssignments"));
				if (_musicStinger.KeyAssignments.Length > 0)
				{
					var clip = _musicStinger.KeyAssignments[0].Clip;
					if (clip)
						RenameAsset(clip.name.Replace("Music_", ""), _musicStinger);
				}
			}
			EditorGUILayout.Separator();
		}
	}
	
	[CustomPropertyDrawer(typeof(KeyAssignment))]
	public class KeyAssignmentDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var fullWidth = position.width;
        
			EditorGUIUtility.labelWidth = 25;
			position.width = fullWidth * 0.3f;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("Keys"), new GUIContent("On"));		
		
			position.x += position.width + 3;
			EditorGUIUtility.labelWidth = 37;
			position.width = fullWidth * 0.7f;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("Clip"), new GUIContent("plays"));
        
			EditorGUI.EndProperty();
		}
	}
}