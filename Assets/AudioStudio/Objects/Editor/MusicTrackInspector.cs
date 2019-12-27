using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(MusicTrack)), CanEditMultipleObjects]
	public class MusicTrackInspector : MusicContainerInspector
	{
		private MusicTrack _musicTrack;

		private void OnEnable()
		{
			_musicTrack = target as MusicTrack;
		}

		public override void OnInspectorGUI()
		{		
			serializedObject.Update();		
			AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Platform"));			
			DrawAudioClipData();
			DrawRhythm();	
			DrawTransition(_musicTrack);
			DrawAudioControls(_musicTrack);		
			serializedObject.ApplyModifiedProperties();
			DrawAuditionButtons(_musicTrack);
			AsGuiDrawer.DrawSaveButton(_musicTrack);
		}
		
		protected void DrawRhythm()
		{
			EditorGUILayout.LabelField("Music Settings", EditorStyles.boldLabel);  
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				EditorGUILayout.LabelField("Rhythm & Key Markers"); 
				AsGuiDrawer.DrawList(serializedObject.FindProperty("Markers"));

				if (!_musicTrack.UseDefaultLoopStyle)
				{
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("PickupBeats"));
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("ExitPosition"));
				}
			}
			EditorGUILayout.Separator();
		}

		private void DrawAudioClipData()
		{
			EditorGUILayout.LabelField("Audio Data", EditorStyles.boldLabel);   
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				if (_musicTrack.Platform != Platform.Web)
				{
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Clip"), "Audio Clip", 80);
					if (_musicTrack.Clip)
						EditorGUILayout.LabelField("Sample Rate:  " + _musicTrack.Clip.frequency);
				}
				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Loop Count", GUILayout.Width(80));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("LoopCount"), GUIContent.none, GUILayout.Width(40));
				EditorGUILayout.LabelField("No Audio Tail", GUILayout.Width(100));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("UseDefaultLoopStyle"), GUIContent.none, GUILayout.Width(20));
				GUILayout.EndHorizontal();	
				if (_musicTrack.Clip)
					RenameAsset(_musicTrack.Clip.name.Replace("Music_", ""), _musicTrack);
			}
			EditorGUILayout.Separator();
		}
	}

	[CustomPropertyDrawer(typeof(MusicMarker))]
	public class MusicMarkerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var fullWidth = position.width;
			
			position.width = fullWidth * 0.5f;
			EditorGUIUtility.labelWidth = 80;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("BarNumber"), new GUIContent("Bar Number"));
			
			position.x += position.width + 5;
			position.width = fullWidth * 0.5f - 5;
			EditorGUIUtility.labelWidth = 40;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("KeyCenter"), new GUIContent("Key"));
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tempo", GUILayout.MinWidth(45));
			EditorGUILayout.PropertyField(property.FindPropertyRelative("Tempo"), GUIContent.none, GUILayout.MinWidth(35));
			EditorGUILayout.LabelField("Time", GUILayout.MinWidth(35));
			EditorGUILayout.PropertyField(property.FindPropertyRelative("BeatsPerBar"), GUIContent.none, GUILayout.MinWidth(25));
			EditorGUILayout.LabelField("/", GUILayout.Width(10));
			EditorGUILayout.PropertyField(property.FindPropertyRelative("BeatDuration"), GUIContent.none, GUILayout.MinWidth(30));
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(" ");
		}
	}
}