using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(SoundClip)), CanEditMultipleObjects]
	public class SoundClipInspector : SoundContainerInspector
	{
		private SoundClip _soundClip;

		private void OnEnable()
		{
			_soundClip = target as SoundClip;
		}

		public override void OnInspectorGUI () {
		
			serializedObject.Update();		
			DrawHierarchy(_soundClip);
			DrawAudioClipData();		
			Draw3DSetting(_soundClip);					
			DrawAudioControls(_soundClip);
			DrawVoiceManagement(_soundClip);
			serializedObject.ApplyModifiedProperties();
			DrawAuditionButtons(_soundClip);
			AsGuiDrawer.DrawSaveButton(_soundClip);
		}

		private void DrawAudioClipData()
		{
			EditorGUILayout.LabelField("Audio Data", EditorStyles.boldLabel);    
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{				
				DrawProperty("Loop", "", 80);
				if (_soundClip.Loop) 
					DrawProperty("SeekRandomPosition", "Seek Random Position", 150);
				DrawProperty("Clip", "Audio Clip", 80);
				if (_soundClip.Clip)
				{ 
					EditorGUILayout.LabelField("Sample Rate:  " + _soundClip.Clip.frequency);
					RenameAsset(_soundClip.Clip.name, _soundClip);
				}
			}			
			EditorGUILayout.Separator();
		}				
	}
}

