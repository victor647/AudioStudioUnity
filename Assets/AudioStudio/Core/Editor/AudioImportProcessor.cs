using System.IO;
using UnityEditor;
using UnityEngine;

namespace AudioStudio
{
	public class AudioImportProcessor : AssetPostprocessor
	{
		public static int MusicQuality = 50;
		public static int SoundQuality = 40;
		public static int VoiceQuality = 30;
		public static int StreamDurationThreshold = 5;
		
		private void OnPreprocessAudio()
		{
			var audio = assetImporter as AudioImporter;
			if (!audio) return;
			audio.forceToMono = false;
			audio.loadInBackground = true;
			audio.ambisonic = false;
			audio.preloadAudioData = false;

			var defaultSampleSettings = new AudioImporterSampleSettings
			{
				compressionFormat = AudioCompressionFormat.Vorbis, sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate
			};

			var audioName = Path.GetFileName(assetPath);
			if (string.IsNullOrEmpty(audioName)) return;
			
			if (audioName.StartsWith("Music_"))
			{
				defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
				defaultSampleSettings.quality = MusicQuality / 100f;
			}
			else if (audioName.StartsWith("Ambience_"))
			{
				defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
				defaultSampleSettings.quality = SoundQuality / 100f;
			}
			else if (audioName.StartsWith("Vo_"))
			{
				defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
				defaultSampleSettings.quality = VoiceQuality / 100f;
			}
			else
			{
				var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
				defaultSampleSettings.loadType = clip.length > StreamDurationThreshold ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
				defaultSampleSettings.quality = SoundQuality / 100f;
			}
			audio.defaultSampleSettings = defaultSampleSettings;			
		}
	}
}