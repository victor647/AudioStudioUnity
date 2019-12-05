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
        public static void PlaySound(string eventName, GameObject soundSource = null, float fadeInTime = -1f, 
            Action<GameObject> callback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (sc)
            {
                var emitter = ValidateSoundSource(soundSource, sc);
                if (sc.EnableVoiceLimit && sc.ReachVoiceLimit(emitter, trigger)) return;
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.PostEvent, trigger, sc.name, soundSource);
                sc.PostEvent(emitter, fadeInTime, callback, trigger);
            }
            else
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SFX, AudioAction.PostEvent, trigger, eventName, soundSource, "Event not found");                         
        }

        public static void StopSound(string eventName, GameObject soundSource = null, float fadeOutTime = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (sc)
            {
                var emitter = ValidateSoundSource(soundSource, sc);
                sc.Stop(emitter, fadeOutTime);     
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.StopEvent, trigger, eventName, soundSource);
            }
            else                           
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SFX, AudioAction.StopEvent, trigger, eventName, soundSource, "Event not found");            
        }

        public static void StopAll(GameObject soundSource = null, float fadeOutTime = 0f)
        {
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var instances = soundSource.GetComponentsInChildren<AudioEventInstance>();
            if (instances.Length > 0)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.StopEvent, AudioTriggerSource.Code, "Stop All", soundSource);
                foreach (var sci in instances)
                {
                    sci.Stop(fadeOutTime);
                }
            }
            else
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SFX, AudioAction.StopEvent, AudioTriggerSource.Code, "Stop All", soundSource, "No playing instance found");
        }
        
        public static void StopAll(string eventName, float fadeOutTime = 0f)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AsAssetLoader.GetSoundEvent(eventName);
            if (sc)
                sc.StopAll(fadeOutTime);  			
            else                           
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SFX, AudioAction.StopEvent, AudioTriggerSource.Code, eventName, null, "SoundEvent not found");            
        }
        
        private static GameObject ValidateSoundSource(GameObject soundSource, SoundContainer soundContainer = null)
        {
            if (soundSource && soundContainer && soundContainer.Is3D)
                return soundSource;
            return GlobalAudioEmitter.GameObject;
        }
        #endregion

        #region Music		
        private static string _lastPlayedMusic;
        private static string _currentPlayingMusic;

        public static void PlayLastMusic()
        {
            PlayMusic(_lastPlayedMusic);
        }
        
        public static void PlayMusic(string eventName, float fadeInTime = -1f, float fadeOutTime = -1f, float exitOffset = -1f, float entryOffset = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName) || eventName == _currentPlayingMusic) return;                

#if UNITY_EDITOR || !UNITY_WEBGL
            var music = AsAssetLoader.LoadMusic(eventName);
            if (!music) return;
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.PostEvent, trigger, music.name);
            MusicTransport.Instance.SetMusicQueue(music, fadeInTime, fadeOutTime, exitOffset, entryOffset, trigger);
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

        public static void StopMusic(float fadeOutTime = 0f)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            MusicTransport.Instance.Stop(fadeOutTime);            
#else
            WebMusicPlayer.Instance.StopMusic();
#endif
            _lastPlayedMusic = _currentPlayingMusic;
            _currentPlayingMusic = null;
        }

        public static void PauseMusic(float fadeOutTime = 0f)
        {
            MusicTransport.Instance.Pause(fadeOutTime);
        }

        public static void ResumeMusic(float fadeInTime = 0f)
        {
            MusicTransport.Instance.Resume(fadeInTime);
        }
        
        public static void MuteMusic(float fadeOutTime = 0f)
        {
            MusicTransport.Instance.Mute(fadeOutTime);
        }
        
        public static void UnMuteMusic(float fadeInTime = 0f)
        {
            MusicTransport.Instance.UnMute(fadeInTime);
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
        private static string _currentVoiceEvent;
        public static void PlayVoice(string eventName, GameObject soundSource = null, float fadeInTime = -1f, 
            Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!VoiceEnabled || string.IsNullOrEmpty(eventName))  return;
            var emitter = ValidateSoundSource(soundSource);
#if UNITY_EDITOR || !UNITY_WEBGL
            var voice = AsAssetLoader.LoadVoice(eventName);   
#else
            var voice = AudioAssetLoader.LoadVoiceWeb(eventName);
#endif
            if (!voice) return;
            voice.PostEvent(emitter, fadeInTime, endCallback, trigger);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.PostEvent, trigger, voice.name, soundSource);
            SetCurrentPlayingVoice(eventName);
        }

        public static void StopVoice(string eventName, GameObject soundSource = null, float fadeOutTime = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var voice = AsAssetLoader.LoadVoice(eventName);          
            if (voice)
                voice.Stop(soundSource, fadeOutTime);   
            else                        
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Voice, AudioAction.StopEvent, trigger, eventName, soundSource, "VoiceEvent not found");                           
        }

        internal static void SetCurrentPlayingVoice(string eventName)
        {
            _currentVoiceEvent = eventName;
        }

        public static void StopCurrentVoice()
        {
            StopVoice(_currentVoiceEvent);
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