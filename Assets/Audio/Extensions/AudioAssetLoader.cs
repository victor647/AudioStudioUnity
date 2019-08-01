using System;
using System.Collections.Generic;
using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio
{
	public static class AudioAssetLoader
	{
		private static Dictionary<string, SoundContainer> _soundEvents;
		private static Dictionary<string, VoiceEvent> _voiceEvents;
		private static Dictionary<string, MusicContainer> _musicEvents;
		private static Dictionary<string, MusicInstrument> _musicInstruments;
		private static List<MusicTransitionSegment> _musicTransitionSegments;
		private static Dictionary<string, SoundBank> _soundBanks;
		private static Dictionary<string, AudioParameter> _audioParameters;
		private static Dictionary<string, AudioSwitch> _audioSwitches;

		public static void Init()
		{
			_soundEvents = new Dictionary<string, SoundContainer>();
			_soundBanks = new Dictionary<string, SoundBank>();
			_voiceEvents = new Dictionary<string, VoiceEvent>();
			_musicEvents = new Dictionary<string, MusicContainer>();
			_musicInstruments = new Dictionary<string, MusicInstrument>();
			_musicTransitionSegments = new List<MusicTransitionSegment>();
			LoadAllTransitionSegments();
			_audioSwitches = new Dictionary<string, AudioSwitch>();
			_audioParameters = new Dictionary<string, AudioParameter>();
			_webMusicList = new Dictionary<string, WebMusicInstance>();
		}

		#region SoundBank
		public static void LoadBank(string bankName)
		{
			if (_soundBanks.ContainsKey(bankName))
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.SoundBank, AudioAction.Load, bankName, "Audio Asset Loader", "Bank already loads");
				return;
			}				
			var loadPath = ShortPath(AudioPathSettings.SoundBanksPath) + $"/{AudioManager.Platform}/{bankName}";
			var bank = Resources.Load<SoundBank>(loadPath);
			if (!bank)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.SoundBank, AudioAction.Load, bankName, "Audio Asset Loader", "Bank not found");                                    
				return;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.SoundBank, AudioAction.Load, bankName, "Audio Asset Loader");
			LoadBank(bank);
		}
		
		private static void LoadBank(SoundBank bank)
		{
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
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.SoundBank, AudioAction.Load, bank.name, "Audio Asset Loader", "Bank loads into memory");                                        
		}	
		
		public static void UnloadBank(string bankName)
		{
			if (!_soundBanks.ContainsKey(bankName))
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.SoundBank, AudioAction.Unload, bankName, "Audio Asset Loader", "Bank already unloads or not found");
				return;
			}				
			UnloadBank(_soundBanks[bankName]);
		}

		public static void UnloadBanks(string nameFilter)
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
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.SoundBank, AudioAction.Unload, bank.name, "Audio Asset Loader", "Bank unloads from memory");
			bank.Dispose();
			Resources.UnloadAsset(bank);            			                     
		}		

		public static SoundContainer GetSoundEvent(string eventName)
		{
			return _soundEvents.ContainsKey(eventName) ? _soundEvents[eventName] : null;
		}	
		
		public static AudioParameter GetAudioParameter(string parameterName)
		{
			return _audioParameters.ContainsKey(parameterName) ? _audioParameters[parameterName] : null;
		}	
		
		public static AudioSwitch GetAudioSwitch(string switchName)
		{
			return _audioSwitches.ContainsKey(switchName) ? _audioSwitches[switchName] : null;
		}
		#endregion
		
		#region Music
		public static MusicContainer LoadMusic(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _musicEvents[eventName];
			var loadPath = ShortPath(AudioPathSettings.MusicEventsPath) + "/" + eventName;
			var music = Resources.Load<MusicContainer>(loadPath);
			if (!music)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Music, AudioAction.Load, eventName, "Audio Asset Loader", "Event not found");                                    
				return null;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Music, AudioAction.Load, eventName, "Audio Asset Loader");
			_musicEvents[eventName] = music;
			music.Init();				
			return music;
		}
		
		public static MusicStinger LoadStinger(string stingerName)
		{
			if (_musicEvents.ContainsKey(stingerName)) 
				return _musicEvents[stingerName] as MusicStinger;
			var loadPath = ShortPath(AudioPathSettings.MusicEventsPath) + "/" + stingerName;
			var stinger = Resources.Load<MusicStinger>(loadPath);
			if (!stinger)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Music, AudioAction.Load, stingerName, "Audio Asset Loader", "Stinger not found");                                    
				return null;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Music, AudioAction.Load, stingerName, "Audio Asset Loader");
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
					AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Music, AudioAction.Load, segment.name, "Audio Asset Loader", "Transition segment loads");
					_musicTransitionSegments.Add(segment);
				}				
			}
		}

		public static MusicTransitionSegment GetTransitionSegment(MusicContainer origin, MusicContainer destination)
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

		public static MusicInstrument LoadInstrument(string instrumentName, byte channel = 1)
		{
			if (_musicInstruments.ContainsKey(instrumentName)) 
				return _musicInstruments[instrumentName] as MusicInstrument;
			var loadPath = ShortPath(AudioPathSettings.MusicInstrumentsPath) + "/" + instrumentName;
			var instrument = Resources.Load<MusicInstrument>(loadPath);
			if (!instrument)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Instrument, AudioAction.Load, instrumentName, "Audio Asset Loader", "Instrument not found");                                    
				return null;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Instrument, AudioAction.Load, instrumentName, "Audio Asset Loader");
			_musicInstruments[instrumentName] = instrument;
			instrument.Init(channel);
			return instrument;
		}
		
		public static void UnloadInstrument(string instrumentName)
		{
			
			if (!_musicInstruments.ContainsKey(instrumentName))
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Instrument, AudioAction.Unload, instrumentName, "Audio Asset Loader", "Instrument already unloads or not found");
				return;
			}
			AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Instrument, AudioAction.Unload, instrumentName, "Audio Asset Loader");
			_musicInstruments[instrumentName].Dispose();
			_musicInstruments.Remove(instrumentName);
		}
		#endregion
		
		#region Voice
		public static VoiceEvent LoadVoice(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _voiceEvents[eventName];
			var loadPath = ShortPath(AudioPathSettings.VoiceEventsPath) + $"/{AudioManager.VoiceLanguage}/{eventName}";
			var voice = Resources.Load<VoiceEvent>(loadPath);
			if (!voice)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Voice, AudioAction.Load, eventName, "Audio Asset Loader", "Event not found");                                    
				return null;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.Load, eventName, "Audio Asset Loader");
			_voiceEvents[eventName] = voice;
			voice.Init();	
			return voice;
		}
		#endregion
		
		#region Web
		private static Dictionary<string, WebMusicInstance> _webMusicList;
		
		public static WebMusicInstance LoadMusicWeb(string eventName)
		{
			if (_webMusicList.ContainsKey(eventName)) 
				return _webMusicList[eventName];
			var loadPath = ShortPath(AudioPathSettings.WebEventsPath) + "/Music/" + eventName;
			var music = Resources.Load<MusicTrack>(loadPath);
			if (!music)
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Music, AudioAction.Load, eventName, "Audio Asset Loader", "Event not found");					
				return null;
			}
			var musicInstance = new WebMusicInstance(music, music.DefaultFadeInTime, music.DefaultFadeOutTime);
			_webMusicList.Add(eventName, musicInstance);	
			WebMusicPlayer.Instance.PlayMusic(musicInstance);			
			return null;
		}
		
		public static VoiceEvent LoadVoiceWeb(string eventName)
		{
			if (_musicEvents.ContainsKey(eventName)) 
				return _voiceEvents[eventName];
			var loadPath = ShortPath(AudioPathSettings.WebEventsPath) + $"/Voice/{AudioManager.VoiceLanguage}/{eventName}";
			var voice = Resources.Load<VoiceEvent>(loadPath);
			if (!voice)
			{                                                            
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Voice, AudioAction.Load, eventName, "Audio Asset Loader", "Event not found");                                    
				return null;
			}                        
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.Load, eventName, "Audio Asset Loader");
			_voiceEvents[eventName] = voice;
			voice.Init();	
			return voice;
		}
		
		public static string DataServerUrl;   
		public static string GetClipUrl(string eventName, ObjectType type)
		{
			return DataServerUrl + ShortPath(AudioPathSettings.StreamingClipsPath) + $"/{type}/{eventName}.ogg";
		}
		
		private static string ShortPath(string longPath)
		{
			longPath = longPath.Replace("\\", "/");
			var index = longPath.IndexOf("Audio", StringComparison.Ordinal);
			return index >= 0 ? longPath.Substring(index) : longPath;
		}   
		#endregion
	}
}