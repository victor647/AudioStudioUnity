using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Tools
{
	public static class AsSelectionCommands
	{
		[MenuItem("Assets/AudioStudio/Filter Selection/Sound Containers")]
		private static void SelectSoundContainers()
		{
			var newSelection = new List<Object>(Selection.objects);
			foreach (var obj in Selection.objects)
			{
				if (!(obj is SoundContainer) || obj is SoundClip) newSelection.Remove(obj);
			}
			Selection.objects = newSelection.ToArray();
		}				
		
		[MenuItem("Assets/AudioStudio/Filter Selection/Sound Clips")]
		private static void SelectSoundClips()
		{
			Selection.objects = Selection.objects.OfType<SoundClip>().Cast<Object>().ToArray();
		}
		
		[MenuItem("Assets/AudioStudio/Filter Selection/Music Containers")]
		private static void SelectMusicContainers()
		{
			var newSelection = new List<Object>(Selection.objects);
			foreach (var obj in Selection.objects)
			{
				if (!(obj is MusicContainer) || obj is MusicTrack) newSelection.Remove(obj);
			}
			Selection.objects = newSelection.ToArray();
		}
		
		[MenuItem("Assets/AudioStudio/Filter Selection/Music Tracks")]
		private static void SelectMusicTracks()
		{
			Selection.objects = Selection.objects.OfType<MusicTrack>().Cast<Object>().ToArray();
		}
		
		[MenuItem("Assets/AudioStudio/Filter Selection/Voice Events")]
		private static void SelectVoiceEvents()
		{
			Selection.objects = Selection.objects.OfType<VoiceEvent>().Cast<Object>().ToArray();
		}
		
		[MenuItem("Assets/AudioStudio/Add To SoundBank")]
		private static void AddToSoundBank()
		{			
			var soundList = Selection.objects.OfType<SoundContainer>().ToArray();										
			AddSoundBankWindow.ShowWindow(soundList);
		}

		[MenuItem("Assets/AudioStudio/Select or Generate Events")]
		private static void GenerateEvents()
		{
			var clipList = Selection.objects.OfType<AudioClip>().ToArray();
			foreach (var audioClip in clipList)
			{
				var path = AssetDatabase.GetAssetPath(audioClip);
				if (path.Contains("Music"))
				{
					var savePath = path.Replace(AsPathSettings.OriginalsPath + "/Music", AsPathSettings.MusicEventsPath)
						.Replace(".wav", ".asset").Replace("Music_", "");

					var savePathLong = Application.dataPath + savePath.Substring(6);
					if (!File.Exists(savePathLong))
					{
						var track = ScriptableObject.CreateInstance<MusicTrack>();
						track.name = audioClip.name.Substring(6);
						track.Clip = audioClip;
						AssetDatabase.CreateAsset(track, savePath);
						Selection.activeObject = track;
					}
					else
					{
						var track = AssetDatabase.LoadAssetAtPath<MusicTrack>(savePath);
						Selection.activeObject = track;
					}					
				}
				else if (path.Contains("Voice"))
				{
					var savePath = path.Replace(AsPathSettings.OriginalsPath + "/Voice", AsPathSettings.VoiceEventsPath)
						.Replace(".wav", ".asset").Replace("Vo_", "");

					var savePathLong = Application.dataPath + savePath.Substring(6);
					if (!File.Exists(savePathLong))
					{
						var voiceEvent = ScriptableObject.CreateInstance<VoiceEvent>();
						voiceEvent.name = audioClip.name.Substring(6);
						voiceEvent.Clip = audioClip;
						AssetDatabase.CreateAsset(voiceEvent, savePath);
						Selection.activeObject = voiceEvent;
					}
					else
					{
						var voiceEvent = AssetDatabase.LoadAssetAtPath<VoiceEvent>(savePath);
						Selection.activeObject = voiceEvent;
					}					
				}
				else
				{
					var savePath = path.Replace(AsPathSettings.OriginalsPath + "/Sound", AsPathSettings.SoundEventsPath)
						.Replace(".wav", ".asset");
					var savePathLong = Application.dataPath + savePath.Substring(6);
					if (!File.Exists(savePathLong))
					{
						var sc = ScriptableObject.CreateInstance<SoundClip>();
						sc.name = audioClip.name.Substring(6);
						sc.Clip = audioClip;
						AssetDatabase.CreateAsset(sc, savePath);
						Selection.activeObject = sc;
					}
					else
					{
						var sc = AssetDatabase.LoadAssetAtPath<SoundClip>(savePath);
						Selection.activeObject = sc;
					}
				}
			}
		}
	}
	
	public class AddSoundBankWindow : EditorWindow
	{		
		public static void ShowWindow(SoundContainer[] soundList)
		{
			var window = (AddSoundBankWindow) GetWindow(typeof(AddSoundBankWindow));			
			window.position = new Rect(800, 400, 200, 10);						
			window.titleContent = new GUIContent("Add To SoundBank");
			_soundContainers = soundList;
		}

		private SoundBank _soundBank;		
		private static SoundContainer[] _soundContainers;
		
		private void OnGUI()
		{
			_soundBank = EditorGUILayout.ObjectField(_soundBank, typeof(SoundBank),false) as SoundBank;
			if (GUILayout.Button("Add"))
			{
				if (_soundBank != null)
				{
					foreach (var sc in _soundContainers)
					{
						_soundBank.RegisterEvent(sc);
					}
					EditorUtility.SetDirty(_soundBank);
					AssetDatabase.SaveAssets();
				}								
				Close();
			}			
		}
	}
}