using System;
using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
	internal static class AsAssetLoader
	{
		private static Dictionary<string, SoundContainer> _soundEvents;
		private static Dictionary<string, VoiceEvent> _voiceEvents;
		private static Dictionary<string, MusicContainer> _musicEvents;
		private static Dictionary<string, MusicInstrument> _musicInstruments;
		private static List<MusicTransitionSegment> _musicTransitionSegments;
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
			_musicTransitionSegments = new List<MusicTransitionSegment>();
			//LoadAllTransitionSegments();
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
		internal static void LoadBank(string bankName)
		{
			var loadPath = ShortPath(AsPathSettings.SoundBanksPath) + $"/{AudioManager.Platform}/{bankName}";
			var bank = Resources.Load<SoundBank>(loadPath);
			if (!bank)
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, AudioTriggerSource.Code, bankName, null, "Bank not found");                                    
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
		}

		internal static void UnloadBank(string bankName)
		{
			if (!_soundBanks.ContainsKey(bankName)) return;
			var bank = _soundBanks[bankName];
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
			Resources.UnloadAsset(bank);    
		}

		internal static SoundContainer GetSoundEvent(string eventName)
		{
			return _soundEvents.ContainsKey(eventName) ? _soundEvents[eventName] : null;
		}	
		
		internal static AudioParameter GetAudioParameter(string parameterName)
		{
			return _audioParameters.ContainsKey(parameterName) ? _audioParameters[parameterName] : null;
		}	
		
		internal static AudioSwitch GetAudioSwitch(string switchName)
		{
			return _audioSwitches.ContainsKey(switchName) ? _audioSwitches[switchName] : null;
		}
		#endregion
		
		#region Music
		internal static MusicContainer LoadMusic(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _musicEvents[eventName];
			var loadPath = ShortPath(AsPathSettings.MusicEventsPath) + "/" + eventName;
			var music = Resources.Load<MusicContainer>(loadPath);
			if (!music)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "Event not found");                                    
				return null;
			}                        
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, eventName);
			_musicEvents[eventName] = music;
			music.Init();				
			return music;
		}
		
		internal static MusicStinger LoadStinger(string stingerName)
		{
			if (_musicEvents.ContainsKey(stingerName)) 
				return _musicEvents[stingerName] as MusicStinger;
			var loadPath = ShortPath(AsPathSettings.MusicEventsPath) + "/" + stingerName;
			var stinger = Resources.Load<MusicStinger>(loadPath);
			if (!stinger)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, stingerName, null, "Stinger not found");                                    
				return null;
			}                        
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, stingerName);
			_musicEvents[stingerName] = stinger;
			stinger.Init();	
			MusicTransport.Instance.QueueStinger(stinger);
			return stinger;
		}

		private static void LoadAllTransitionSegments()
		{
			var segments = Resources.LoadAll<MusicTransitionSegment>("Audio/Events/Music/");
			if (segments.Length > 0)
			{
				foreach (var segment in segments)
				{
					AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, segment.name, null, "Transition segment loads");
					_musicTransitionSegments.Add(segment);
				}				
			}
		}

		internal static MusicTransitionSegment GetTransitionSegment(MusicContainer origin, MusicContainer destination)
		{
			foreach (var segment in _musicTransitionSegments)
			{
				if ((segment.Destination == destination && segment.Origin == origin)
				    || (!segment.Destination && segment.Origin == origin)
				    || (segment.Destination == destination && !segment.Origin))
				{
					return segment;
				}
			}
			return null;
		}

		internal static MusicInstrument LoadInstrument(string instrumentName, byte channel = 1)
		{
			if (_musicInstruments.ContainsKey(instrumentName)) 
				return _musicInstruments[instrumentName] as MusicInstrument;
			var loadPath = ShortPath(AsPathSettings.MusicInstrumentsPath) + "/" + instrumentName;
			var instrument = Resources.Load<MusicInstrument>(loadPath);
			if (!instrument)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Instrument, AudioAction.Load, AudioTriggerSource.Code, instrumentName, null, "Instrument not found");                                    
				return null;
			}                        
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Instrument, AudioAction.Load, AudioTriggerSource.Code, instrumentName);
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
			AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Instrument, AudioAction.Unload, AudioTriggerSource.Code, instrumentName);
			_musicInstruments[instrumentName].Dispose();
			_musicInstruments.Remove(instrumentName);
		}
		#endregion
		
		#region Voice
		internal static VoiceEvent LoadVoice(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _voiceEvents[eventName];
			var loadPath = ShortPath(AsPathSettings.VoiceEventsPath) + $"/{AudioManager.VoiceLanguage}/{eventName}";
			var voice = Resources.Load<VoiceEvent>(loadPath);
			if (!voice)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Voice, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "Event not found");                                    
				return null;
			}                        
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Load, AudioTriggerSource.Code, eventName);
			_voiceEvents[eventName] = voice;
			voice.Init();	
			return voice;
		}
		#endregion
		
		#region Web
#if !UNITY_EDITOR && UNITY_WEBGL		
		private static Dictionary<string, WebMusicInstance> _webMusicList = new Dictionary<string, WebMusicInstance>();
		
		internal static WebMusicInstance LoadMusicWeb(string eventName)
		{
			if (_webMusicList.ContainsKey(eventName)) 
				return _webMusicList[eventName];
			var loadPath = ShortPath(AsPathSettings.WebEventsPath) + "/Music/" + eventName;
			var music = Resources.Load<MusicTrack>(loadPath);
			if (!music)
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "Event not found");					
				return null;
			}
			var musicInstance = new WebMusicInstance(music, music.DefaultFadeInTime, music.DefaultFadeOutTime);
			_webMusicList.Add(eventName, musicInstance);	
			WebMusicPlayer.Instance.PlayMusic(musicInstance);			
			return null;
		}
		
		internal static VoiceEvent LoadVoiceWeb(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _voiceEvents[eventName];
			var loadPath = ShortPath(AsPathSettings.WebEventsPath) + $"/Voice/{AudioManager.VoiceLanguage}/{eventName}";
			var voice = Resources.Load<VoiceEvent>(loadPath);
			if (!voice)
			{                                                            
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Voice, AudioAction.Load, AudioTriggerSource.Code, eventName, null, "Event not found");                                    
				return null;
			}                        
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Load, AudioTriggerSource.Code, eventName);
			_voiceEvents[eventName] = voice;
			voice.Init();	
			return voice;
		}
		
		internal static string DataServerUrl;   
		internal static string GetClipUrl(string eventName, ObjectType type)
		{
			return DataServerUrl + ShortPath(AsPathSettings.StreamingClipsPath) + $"/{type}/{eventName}.ogg";
		}
#endif
		#endregion
	}
}