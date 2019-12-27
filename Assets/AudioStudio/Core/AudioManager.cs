using UnityEngine;
using System;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine.Audio;

namespace AudioStudio
{		
    public static class AudioManager
    {		        
        public static Platform Platform;

        #region Sound           
        public static void PlaySound(string eventName, GameObject soundSource = null, float fadeInTime = 0f, 
            Action<GameObject> callback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            if (sc.EnableVoiceLimit && sc.ReachVoiceLimit(emitter, trigger)) return;
            sc.Play(emitter, fadeInTime, callback);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, trigger, sc.name, soundSource);
        }

        public static void StopSound(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sc.Stop(emitter, fadeOutTime);     
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, trigger, eventName, soundSource);
        }

        public static void StopAll(GameObject soundSource = null, float fadeOutTime = 0f)
        {
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var instances = soundSource.GetComponentsInChildren<AudioEventInstance>();
            if (instances.Length > 0)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, AudioTriggerSource.Code, "Stop All", soundSource);
                foreach (var sci in instances)
                {
                    sci.Stop(fadeOutTime);
                }
            }
            else
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SFX, AudioAction.Stop, AudioTriggerSource.Code, "Stop All", soundSource, "No playing instance found");
        }
        
        public static void StopAll(string eventName, float fadeOutTime = 0f)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (sc)
                sc.StopAll(fadeOutTime);
        }
        
        public static void MuteSound(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sc.Mute(emitter, fadeOutTime);     
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Mute, trigger, eventName, soundSource);
        }
        
        public static void UnMuteSound(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sc.UnMute(emitter, fadeInTime);     
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.UnMute, trigger, eventName, soundSource);
        }
        
        public static void PauseSound(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sc.Pause(emitter, fadeOutTime);     
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Pause, trigger, eventName, soundSource);
        }
        
        public static void ResumeSound(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (!sc) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sc.Resume(emitter, fadeInTime);     
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Resume, trigger, eventName, soundSource);
        }
        
        private static GameObject ValidateSoundSource(GameObject soundSource, AudioTriggerSource trigger)
        {
            switch (trigger)
            {
                case AudioTriggerSource.ButtonSound:
                case AudioTriggerSource.DropdownSound:
                case AudioTriggerSource.EventSound:
                case AudioTriggerSource.InspectorAudition:
                case AudioTriggerSource.MenuSound:
                case AudioTriggerSource.ScrollSound:
                case AudioTriggerSource.SliderSound:
                case AudioTriggerSource.ToggleSound:
                    return GlobalAudioEmitter.GameObject;
                default:
                    return soundSource ? soundSource : GlobalAudioEmitter.GameObject;
            }
        }
        #endregion

        #region Music		
        private static string _lastPlayedMusic;
        private static string _currentPlayingMusic;

        public static void PlayLastMusic()
        {
            PlayMusic(_lastPlayedMusic);
        }
        
        public static void PlayMusic(string eventName, float fadeInTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName) || eventName == _currentPlayingMusic) return;                

#if UNITY_EDITOR || !UNITY_WEBGL
            var music = AsAssetLoader.LoadMusic(eventName);
            if (!music) return;
            MusicTransport.Instance.SetMusicQueue(music, fadeInTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Play, trigger, eventName, source);
#else
            var music = AsAssetLoader.LoadMusicWeb(eventName);
            if (!music) return;
            WebMusicPlayer.Instance.PlayMusic(music);
#endif
            UpdateLastMusic(eventName);
        }

        internal static void UpdateLastMusic(string musicName)
        {
            _lastPlayedMusic = string.IsNullOrEmpty(_currentPlayingMusic) ? musicName : _currentPlayingMusic;
            _currentPlayingMusic = musicName;
        }

        public static void StopMusic(float fadeOutTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            MusicTransport.Instance.Stop(fadeOutTime);       
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Stop, trigger, _currentPlayingMusic, source, fadeOutTime + "s fade out");
#else
            WebMusicPlayer.Instance.StopMusic();
#endif
            _lastPlayedMusic = _currentPlayingMusic;
            _currentPlayingMusic = string.Empty;
        }
        
        public static void MuteMusic(float fadeOutTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.Mute(fadeOutTime);
            CheckIfMusicPlaying(AudioAction.Mute, source, trigger);
        }
        
        public static void UnMuteMusic(float fadeInTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.UnMute(fadeInTime);
            CheckIfMusicPlaying(AudioAction.UnMute, source, trigger);
        }

        public static void PauseMusic(float fadeOutTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.Pause(fadeOutTime);
            CheckIfMusicPlaying(AudioAction.Pause, source, trigger);
        }

        public static void ResumeMusic(float fadeInTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.Resume(fadeInTime);
            CheckIfMusicPlaying(AudioAction.Resume, source, trigger);
        }

        private static void CheckIfMusicPlaying(AudioAction action, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (MusicTransport.Instance.PlayingStatus != PlayingStatus.Idle)
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, action, trigger, _currentPlayingMusic, source);
            else
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Music, action, trigger, _currentPlayingMusic, source, "No music is playing!");
        }

        public static void PlayStinger(string stingerName)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            if (string.IsNullOrEmpty(stingerName)) return;            
            var stinger = AsAssetLoader.LoadStinger(stingerName);          
            if (stinger)
                MusicTransport.Instance.QueueStinger(stinger);             
#endif               
        }

        public static void PlayInstrument(string instrumentName, byte channel = 1)
        {
            AsAssetLoader.LoadInstrument(instrumentName, channel);
        }
        
        public static void StopInstrument(string instrumentName)
        {
            AsAssetLoader.UnloadInstrument(instrumentName);
        }
        #endregion

        #region Voice        
        private static string _currentPlayingVoice;
        public static void PlayVoice(string eventName, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!VoiceEnabled || string.IsNullOrEmpty(eventName))  return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.Play(emitter, fadeInTime, endCallback);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Play, trigger, voice.name, soundSource);
            SetCurrentPlayingVoice(eventName);
        }

        public static void StopVoice(string eventName = null, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = _currentPlayingVoice;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.Stop(emitter, fadeOutTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Stop, trigger, eventName, soundSource);
        }
        
        public static void MuteVoice(string eventName = null, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = _currentPlayingVoice;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.Mute(emitter, fadeOutTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Mute, trigger, eventName, soundSource);
        }
        
        public static void UnMuteVoice(string eventName = null, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = _currentPlayingVoice;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.UnMute(emitter, fadeInTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.UnMute, trigger, eventName, soundSource);
        }
        
        public static void PauseVoice(string eventName = null, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = _currentPlayingVoice;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.Pause(emitter, fadeOutTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Pause, trigger, eventName, soundSource);
        }
        
        public static void ResumeVoice(string eventName = null, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = _currentPlayingVoice;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var voice = AsAssetLoader.LoadVoice(eventName);
            if (!voice) return;
            voice.Resume(emitter, fadeInTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Resume, trigger, eventName, soundSource);
        }

        internal static void SetCurrentPlayingVoice(string eventName)
        {
            _currentPlayingVoice = eventName;
        }
        #endregion       
		
        #region Switch		
        public static void SetSwitchGlobal(string switchGroupName, string switchName, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitchGlobal(switchName);
            else                            
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, switchGroupName, null, "Switch not found");                                    
        }

        public static void SetSwitch(string switchGroupName, string switchName, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;  
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitch(switchName, affectedGameObject);
            else                            
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, switchGroupName, affectedGameObject, "Switch not found");                                                               
        }

        public static string GetSwitch(string switchGroupName, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName)) return null;      
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                return swc.GetSwitch(affectedGameObject);                                
            AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.GetValue, trigger, switchGroupName, affectedGameObject, "Switch not found");   	
            return "";
        }
        #endregion
	
        #region Parameter				
        public static void SetParameterValueGlobal(string parameterName, float parameterValue, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValueGlobal(parameterValue);   
            else
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Parameter, AudioAction.SetValue, trigger, parameterName, null, "Parameter not found");                                    
        }

        public static void SetParameterValue(string parameterName, float parameterValue, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {                        
            if (string.IsNullOrEmpty(parameterName)) return;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValue(parameterValue, affectedGameObject);   
            else           
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Parameter, AudioAction.SetValue, trigger, parameterName, null, "Parameter not found");            
        }

        public static float GetParameterValue(string parameterName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(parameterName)) return 0f;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                return ap.GetValue(affectedGameObject);         
            AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Parameter, AudioAction.GetValue, AudioTriggerSource.Code, parameterName, null, "Parameter not found");	
            return 0f;
        }
        #endregion
		
        #region SoundBank
        public static void LoadBank(string bankName, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            BankManager.LoadBank(bankName, trigger);
        }

        public static void UnloadBank(string bankName, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            BankManager.UnloadBank(bankName, trigger);                                                   
        }  
        
        public static void UnloadBanks(string nameFilter)
        {
            BankManager.UnloadBanks(nameFilter);                                                   
        }   
        #endregion
                
        #region PreferenceSetting
        public static AudioMixer AudioMixer;

        public static void LoadPreferenceSettings()
        {
            SoundEnabled = SoundEnabled;
            MusicEnabled = MusicEnabled;
            VoiceEnabled = VoiceEnabled;
            SoundVolume = SoundVolume;
            MusicVolume = MusicVolume;
            VoiceVolume = VoiceVolume;
        }
		
        public static AudioMixerGroup GetAudioMixer(string type, string subMixer = "")
        {
            return !AudioMixer ? null : AudioMixer.FindMatchingGroups("Master/" + type + (string.IsNullOrEmpty(subMixer) ? "" : "/" + subMixer))[0];
        }

        public static bool SoundEnabled
        {
            get
            {
                var enabled = PlayerPrefs.GetInt("AUDIO_SOUND", 1);
                return enabled == 1;
            }
            set
            {
                var volume = value ? SoundVolume : 0f;
                var decibel = LinearToDecibel(volume);
                AudioMixer.SetFloat("SoundVolume", decibel);
                PlayerPrefs.SetInt("AUDIO_SOUND", value ? 1: 0);                   
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Sound On" : "Sound Off");
            }
        }
        
        public static bool VoiceEnabled
        {
            get
            {
                var enabled = PlayerPrefs.GetInt("AUDIO_VOICE", 1);
                return enabled == 1;
            }
            set
            {
                var volume = value ? VoiceVolume : 0f;
                var decibel = LinearToDecibel(volume);
                AudioMixer.SetFloat("VoiceVolume", decibel);
                PlayerPrefs.SetInt("AUDIO_VOICE", value ? 1: 0);        
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Voice On" : "Voice Off");
            }
        }
        
        public static bool MusicEnabled
        {
            get
            {
                var enabled = PlayerPrefs.GetInt("AUDIO_MUSIC", 1);
                return enabled == 1;
            }
            set
            {
                var volume = value ? MusicVolume : 0f;
                var decibel = LinearToDecibel(volume);
                AudioMixer.SetFloat("MusicVolume", decibel);
                PlayerPrefs.SetInt("AUDIO_MUSIC", value ? 1: 0);             
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Music On" : "Music Off");
            }
        }

        public static float SoundVolume
        {
            get
            {
                return PlayerPrefs.GetFloat("SOUND_VOLUME", 100f);
            }
            set
            {
                var volume = Mathf.Clamp(value, 0f, 100f);
                var decibel = LinearToDecibel(volume);
                if (SoundEnabled)
                    AudioMixer.SetFloat("SoundVolume", decibel);
                PlayerPrefs.SetFloat("SOUND_VOLUME", volume);                          
            }
        }

        public static float VoiceVolume
        {
            get
            {
                return PlayerPrefs.GetFloat("VOICE_VOLUME", 100f);
            }
            set
            {
                var volume = Mathf.Clamp(value, 0f, 100f);
                var decibel = LinearToDecibel(volume);
                if (VoiceEnabled)
                    AudioMixer.SetFloat("VoiceVolume", decibel);
                PlayerPrefs.SetFloat("VOICE_VOLUME", volume);                
            }
        }
        
        public static float MusicVolume
        {
            get
            {
                return PlayerPrefs.GetFloat("MUSIC_VOLUME", 100f);
            }
            set
            {                
                var volume = Mathf.Clamp(value, 0f, 100f);
                var decibel = LinearToDecibel(volume);
                if (MusicEnabled)
                    AudioMixer.SetFloat("MusicVolume", decibel);
                PlayerPrefs.SetFloat("MUSIC_VOLUME", volume);                   
            }
        }
        
        private static float LinearToDecibel(float linear)
        {
            return Mathf.Max(-80f, 20f * Mathf.Log10(linear) - 40f);
        }

        public static Languages VoiceLanguage
        {
            get
            {
                var i = PlayerPrefs.GetInt("VOICE_LANGUAGE", 0);                
                return (Languages)i;
            }
            set
            {
                if (VoiceLanguage == value) return;
                PlayerPrefs.SetInt("VOICE_LANGUAGE", (int)value);                          
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Language, AudioAction.SetValue, AudioTriggerSource.Code, value.ToString());
            }
        }        
        #endregion
        
        #region Listener
        public static void SyncTransformWithParent(GameObject source, GameObject parent = null)
        {
            var parentTransform = parent ? parent.transform : source.transform.parent;
            if (parent)
                source.transform.parent = parentTransform;                                       
            source.transform.position = parentTransform.position;
            source.transform.rotation = parentTransform.rotation;
        }
        #endregion
        
    }			
}