using System;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
	internal static partial class AsAssetLoader
	{
		public static void LoadAudioInitData()
		{
			var config = Resources.Load<AudioInitLoadData>("AudioStudio/AudioInitLoadData");
			if (config)
				config.LoadAudioData();
		}

		#region SoundBank
		internal static void LoadBank(string bankName, Action<SoundBank, BankLoadStatus> onLoadFinished)
		{
			var loadPath = ShortPath(AudioPathSettings.Instance.SoundBanksPath) + "/" + bankName;
			var bank = Resources.Load<SoundBank>(loadPath);
			if (!bank)
			{
				onLoadFinished(bank, BankLoadStatus.NotFound);
				return;
			}
			DoLoadBank(bank);
			onLoadFinished(bank, BankLoadStatus.Loaded);
		}
		
		internal static void DoLoadBank(SoundBank bank)
		{
			foreach (var ac in bank.AudioControllers)
			{
				if (!ac)
				{
					Debug.LogError("AudioController of SoundBank " + bank.name + " is missing!");
					continue;
				}

				ac.Init();
				var parameter = ac as AudioParameter;
				if (parameter != null) _audioParameters[parameter.name] = parameter;
				var audioSwitch = ac as AudioSwitch;
				if (audioSwitch != null) _audioSwitches[audioSwitch.name] = audioSwitch;
			}

			foreach (var evt in bank.AudioEvents)
			{
				if (!evt)
				{
					Debug.LogError("AudioEvent of SoundBank " + bank.name + " is missing!");
					continue;
				}

				evt.Init();
				_audioEvents[evt.name] = evt;
			}
		}

		internal static void UnloadBank(SoundBank bank)
		{
			foreach (var evt in bank.AudioEvents)
			{       
				if (!evt)
				{
					Debug.LogError("AudioEvent of SoundBank " + bank.name + " is missing!");
					continue;
				}
				_audioEvents.Remove(evt.name);
				evt.StopAll(0);				
				evt.Dispose();
			}
			foreach (var ac in bank.AudioControllers)
			{			
				if (!ac)
				{
					Debug.LogError("AudioController of SoundBank " + bank.name + " is missing!");
					continue;
				}	
				if (ac is AudioParameter) 
					_audioParameters.Remove(ac.name);
				if (ac is AudioSwitch) 
					_audioSwitches.Remove(ac.name);    
				ac.Dispose();
			}
			Resources.UnloadAsset(bank);
		}
		#endregion
		
		#region Instrument
		internal static MusicInstrument LoadInstrument(string instrumentName, byte channel = 1)
		{
			if (_musicInstruments.ContainsKey(instrumentName)) 
				return _musicInstruments[instrumentName];
			var loadPath = ShortPath(AudioPathSettings.Instance.MusicInstrumentsPath) + "/" + instrumentName;
			var instrument = Resources.Load<MusicInstrument>(loadPath);
			if (instrument)
			{
				_musicInstruments[instrumentName] = instrument;
				instrument.Init(channel);
			}
			else
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Instrument, AudioAction.Load, AudioTriggerSource.Code, instrumentName, null, "Instrument not found");                                    
			return instrument;
		}
		
		internal static void UnloadInstrument(string instrumentName)
		{
			if (!_musicInstruments.ContainsKey(instrumentName))
			{
				AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Instrument, AudioAction.Unload, AudioTriggerSource.Code, instrumentName, null, "Instrument already unloads or not found");
				return;
			}
			AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Instrument, AudioAction.Unload, AudioTriggerSource.Code, instrumentName);
			_musicInstruments[instrumentName].Dispose();
			_musicInstruments.Remove(instrumentName);
		}
		#endregion

		#region Web
#if !UNITY_EDITOR && UNITY_WEBGL		
		private static Dictionary<string, WebMusicInstance> _webMusicList = new Dictionary<string, WebMusicInstance>();
		
		internal static WebMusicInstance LoadMusicWeb(string eventName)
		{
			if (string.IsNullOrEmpty(eventName))
				return null;
			if (_webMusicList.ContainsKey(eventName)) 
				return _webMusicList[eventName];
			var loadPath = ShortPath(AudioPathSettings.Instance.WebEventsPath) + "/Music/" + eventName;
			var music = Resources.Load<MusicTrack>(loadPath);
			if (music)
			{
				var musicInstance = new WebMusicInstance(music, 1f, 1f);
				_webMusicList.Add(eventName, musicInstance);
			}
			else
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "Music not found");					
			return music;
		}

		internal static string DataServerUrl;   
		internal static string GetClipUrl(string eventName, AudioObjectType type)
		{
			return DataServerUrl + ShortPath(AudioPathSettings.Instance.StreamingClipsPath) + string.Format("/{0}/{1}.ogg", type, eventName);
		}
#endif
		#endregion
	}
}