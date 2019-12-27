using System.Linq;
using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{	
	[CustomEditor(typeof(VoiceEvent)), CanEditMultipleObjects]
	public class VoiceEventInspector : AudioEventInspector
	{
		private VoiceEvent _voiceEvent;

		private void OnEnable()
		{
			_voiceEvent = target as VoiceEvent;
		}
		
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Platform"));						
			EditorGUILayout.Separator();			
			DrawAudioClips();		
			DrawAudioControls();
			serializedObject.ApplyModifiedProperties();
			DrawAuditionButtons(_voiceEvent);
			AsGuiDrawer.DrawSaveButton(_voiceEvent);
		}

		private void DrawAudioClips()
		{			
			EditorGUILayout.LabelField("Audio Data", EditorStyles.boldLabel, GUILayout.Width(150)); 
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {				            
	            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("PlayLogic"));
	            if (_voiceEvent.Platform != Platform.Web)
	            {
		            switch (_voiceEvent.PlayLogic)
		            {
			            case VoicePlayLogic.Single:
				            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Clip"), "Audio Clip");
				            if (_voiceEvent.Clip)
					            EditorGUILayout.LabelField("Sample Rate:  " + _voiceEvent.Clip.frequency);
				            break;
			            case VoicePlayLogic.Random:
				            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AvoidRepeat"), "  Avoid Repeat", 120);  
				            EditorGUILayout.LabelField("Audio Clips");
				            AsGuiDrawer.DrawList(serializedObject.FindProperty("Clips"), "", AddChildClip);
				            break;
			            case VoicePlayLogic.SequenceStep:
				            EditorGUILayout.LabelField("Audio Clips");
				            AsGuiDrawer.DrawList(serializedObject.FindProperty("Clips"), "", AddChildClip);
				            break;
			            case VoicePlayLogic.Switch:
				            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AudioSwitchName"), "Audio Switch");
				            EditorGUILayout.LabelField("Switch Assignment");
				            AsGuiDrawer.DrawList(serializedObject.FindProperty("SwitchClipMappings"));
				            break;
		            }

		            if (GUILayout.Button("Rename By AudioClip"))
		            {
			            if (_voiceEvent.Clips.Count > 0)
				            RenameAsset(_voiceEvent.Clips[0].name, _voiceEvent);
			            else if (_voiceEvent.Clip != null)
				            RenameAsset(_voiceEvent.Clip.name, _voiceEvent);
			            else
				            EditorUtility.DisplayDialog("Error", "No AudioClip found!", "OK");
		            }
	            }
	            else
	            {
		            switch (_voiceEvent.PlayLogic)
		            {
			            case VoicePlayLogic.Random:
			            case VoicePlayLogic.SequenceStep:
				            EditorGUILayout.PropertyField(serializedObject.FindProperty("ClipCount"));
				            break;
		            }
	            }
            }
			EditorGUILayout.Separator();
        }
		
		private void AddChildClip(Object[] objects)
		{
			var clips = objects.Select(obj => obj as AudioClip).Where(a => a).ToArray();                   
			foreach (var clip in clips)
			{
				_voiceEvent.Clips.Add(clip);
			}								
		}
		
		private void DrawAudioControls()
		{			
			EditorGUILayout.LabelField("Audio Controls", EditorStyles.boldLabel, GUILayout.Width(150)); 
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Volume"));
				if (_voiceEvent.Platform != Platform.Web)
				{
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Pitch"));
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Pan"));
					DrawFilters(_voiceEvent);
					DrawSubMixer(_voiceEvent);
					DrawParameterMappings();
				}
			}
		}
	}
}