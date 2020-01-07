using System;
using System.Collections.Generic;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using Core;
using UnityEngine;

namespace AudioStudio
{
	internal static class AsAssetLoader
	{
		private static Dictionary<string, SoundContainer> _soundEvents;
		private static Dictionary<string, VoiceEvent> _voiceEvents;
		private static Dictionary<string, MusicContainer> _musicEvents;
		private static Dictionary<string, MusicInstrument> _musicInstruments;
		private static Dictionary<string, SoundBank> _soundBanks;
		private static Dictionary<string, AudioParameter> _audioParameters;
		private static Dictionary<string, AudioSwitch> _audioSwitches;

		internal static void Init()
		{
			_soundEvents = new Dictionary<string, SoundContainer>();
			_soundBanks = new Dictionary<string, SoundBank>();
			_voiceEvents = new Dictionary<string, VoiceEvent>();
			_musicEvents = new Dictionary<string, MusicContainer>();
			_musicInstruments = new Dictionary<string, MusicInstrument>();
			_audioSwitches = new Dictionary<string, AudioSwitch>();
			_audioParameters = new Dictionary<string, AudioParameter>();
		}
		
		private static string ShortPath(string longPath)
		{
			longPath = longPath.Replace("\\", "/");
			var index = longPath.IndexOf("Audio", StringComparison.Ordinal);
			return index >= 0 ? longPath.Substring(index) : longPath;
		}   

		#region SoundBank
		internal static bool LoadBank(string bankName, PostEventReference finishedPostEvent = null)
		{
			if (string.IsNullOrEmpty(bankName) || _soundBanks.ContainsKey(bankName))
				return false;
			var loadPath = ShortPath(AudioPathSettings.Instance.SoundBanksPath) + $"/{AudioManager.Platform}/{bankName}";
			ResourceManager.Instance.Load<SoundBank>(loadPath, bank =>
			{
				if (!bank)
				{
					AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, AudioTriggerSource.Code, "N/A", null, "Bank not found");
					return;
				}
				_soundBanks[bank.name] = bank;
				bank.Init();
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
					_soundEvents[evt.name] = evt;
				}            
				finishedPostEvent?.Post();
			});
			return true;
		}

		internal static bool UnloadBank(string bankName)
		{
			if (!_soundBanks.ContainsKey(bankName))
				return false;
			UnloadBank(_soundBanks[bankName]);
			return true;
		}

		internal static void UnloadBanks(string nameFilter)
		{
			var loadedBanks = new List<string>(_soundBanks.Keys);
			foreach (var bank in loadedBanks)
			{
				if (bank.Contains(nameFilter))
					UnloadBank(bank);
			}
		}
		
		private static void UnloadBank(SoundBank bank)
		{            			
			foreach (var evt in bank.AudioEvents)
			{       
				if (!evt)
				{
					Debug.LogError("AudioEvent of SoundBank " + bank.name + " is missing!");
					continue;
				}
				_soundEvents.Remove(evt.name);
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
				if (ac is AudioParameter) _audioParameters.Remove(ac.name);
				if (ac is AudioSwitch) _audioSwitches.Remove(ac.name);    
				ac.Dispose();
			}
			_soundBanks.Remove(bank.name);
			bank.Dispose();
			//Resources.UnloadAsset(bank);            			                     
		}		

		internal static SoundContainer GetSoundEvent(string eventName)
		{
			if (_soundEvents.ContainsKey(eventName))
				return _soundEvents[eventName];
			AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SFX, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "SFX not loaded");
			return null;
		}	
		
		internal static AudioParameter GetAudioParameter(string parameterName)
		{
			if (_audioParameters.ContainsKey(parameterName))
				return _audioParameters[parameterName];
			AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SFX, AudioAction.Load, AudioTriggerSource.Code, parameterName, null, "Parameter not loaded");
			return null;
		}	
		
		internal static AudioSwitch GetAudioSwitch(string switchName)
		{
			if (_audioSwitches.ContainsKey(switchName))
				return _audioSwitches[switchName];
			AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SFX, AudioAction.Load, AudioTriggerSource.Code, switchName, null, "Switch not loaded");
			return null;
		}
		#endregion
		
		#region Music
		internal static MusicContainer LoadMusic(string eventName)
		{
			if (string.IsNullOrEmpty(eventName))
				return null;
			if (_musicEvents.ContainsKey(eventName)) 
				return _musicEvents[eventName];
			var loadPath = ShortPath(AudioPathSettings.Instance.MusicEventsPath) + "/" + eventName;
			ResourceManager.Instance.Load<MusicContainer>(loadPath, LoadMusicAndPlay);
			return null;
		}

		private static void LoadMusicAndPlay(MusicContainer music)
		{
			if (!music)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, "N/A", null, "Event not found");                                    
				return;
			}
			_musicEvents[music.name] = music;
			music.Init();
			music.Play(null, -1f);
			AudioManager.UpdateLastMusic(music.name);
		}
		
		internal static MusicStinger LoadStinger(string stingerName)
		{
			if (string.IsNullOrEmpty(stingerName))
				return null;
			if (_musicEvents.ContainsKey(stingerName)) 
				return _musicEvents[stingerName] as MusicStinger;
			var loadPath = ShortPath(AudioPathSettings.Instance.MusicEventsPath) + "/" + stingerName;
			var stinger = Resources.Load<MusicStinger>(loadPath);
			if (!stinger)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, stingerName, null, "Stinger not found");                                    
				return null;
			}
			_musicEvents[stingerName] = stinger;
			stinger.Init();	
			MusicTransport.Instance.QueueStinger(stinger);
			return stinger;
		}

		internal static MusicInstrument LoadInstrument(string instrumentName, byte channel = 1)
		{
			if (string.IsNullOrEmpty(instrumentName))
				return null;
			if (_musicInstruments.ContainsKey(instrumentName)) 
				return _musicInstruments[instrumentName] as MusicInstrument;
			var loadPath = ShortPath(AudioPathSettings.Instance.MusicInstrumentsPath) + "/" + instrumentName;
			var instrument = Resources.Load<MusicInstrument>(loadPath);
			if (!instrument)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Instrument, AudioAction.Load, AudioTriggerSource.Code, instrumentName, null, "Instrument not found");                                    
				return null;
			}
			_musicInstruments[instrumentName] = instrument;
			instrument.Init(channel);
			return instrument;
		}
		
		internal static void UnloadInstrument(string instrumentName)
		{
			if (!_musicInstruments.ContainsKey(instrumentName))
			{
				AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Instrument, AudioAction.Unload, AudioTriggerSource.Code, instrumentName, null, "Instrument already unloads or not found");
				return;
			}
			_musicInstruments[instrumentName].Dispose();
			_musicInstruments.Remove(instrumentName);
		}
		#endregion
		
		#region Voice
		internal static VoiceEvent LoadVoice(string eventName)
		{
			if (string.IsNullOrEmpty(eventName))
				return null;
			if (_musicEvents.ContainsKey(eventName)) 
				return _voiceEvents[eventName];
			var loadPath = ShortPath(AudioPathSettings.Instance.VoiceEventsPath) + $"/{AudioManager.VoiceLanguage}/{eventName}";
			ResourceManager.Instance.Load<VoiceEvent>(loadPath, LoadVoiceAndPlay);
			return null;
		}

		private static void LoadVoiceAndPlay(VoiceEvent voice)
		{
			if (!voice)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Voice, AudioAction.Load, AudioTriggerSource.Code, "N/A", null, "Event not found");                                    
				return;
			}
			_voiceEvents[voice.name] = voice;
			voice.Init();
			voice.Play(GlobalAudioEmitter.GameObject, -1f);
			AudioManager.SetCurrentPlayingVoice(voice.name);
		}
		#endregion
	}
}