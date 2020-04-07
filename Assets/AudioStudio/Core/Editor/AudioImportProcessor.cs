using System.IO;
using UnityEditor;
using UnityEngine;

namespace AudioStudio
{
	public class AudioImportProcessor : AssetPostprocessor
	{
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
				defaultSampleSettings.quality = AudioPathSettings.Instance.MusicQuality / 100f;
				defaultSampleSettings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
			}
			else if (audioName.StartsWith("Ambience_"))
			{
				defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
				defaultSampleSettings.quality = AudioPathSettings.Instance.SoundQuality / 100f;
			}
			else if (audioName.StartsWith("Vo_"))
			{
				defaultSampleSettings.loadType = AudioClipLoadType.Streaming;
				defaultSampleSettings.quality = AudioPathSettings.Instance.VoiceQuality / 100f;
			}
			else
			{
				var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
				defaultSampleSettings.loadType = clip.length > AudioPathSettings.Instance.StreamDurationThreshold ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
				defaultSampleSettings.quality = AudioPathSettings.Instance.SoundQuality / 100f;
			}
			audio.defaultSampleSettings = defaultSampleSettings;			
		}
	}
}