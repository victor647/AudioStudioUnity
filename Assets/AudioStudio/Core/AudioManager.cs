using UnityEngine;
using System;
using System.Collections.Generic;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace AudioStudio
{		
    public static class AudioManager
    {
        #region Sound           
        public static string PlaySound(string eventName, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (string.IsNullOrEmpty(eventName) || !SoundEnabled) return string.Empty;
            var sound = AsAssetLoader.GetAudioEvent(eventName) as SoundContainer;
            if (!sound)
            {
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SFX, AudioAction.Play, trigger, eventName, soundSource, "SFX not loaded");
                return string.Empty;
            }
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = sound.Play(emitter, fadeInTime, endCallback);
            if (!string.IsNullOrEmpty(clipName))
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, trigger, eventName, soundSource);
            return clipName;
        }

        public static void StopSound(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sound = AsAssetLoader.GetAudioEvent(eventName) as SoundContainer;
            if (!sound)
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SFX, AudioAction.Stop, trigger, eventName, soundSource, "SFX not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                sound.Stop(emitter, fadeOutTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, trigger, eventName, soundSource);
            }
        }
        #endregion
        
        #region Music		
        private static string _lastPlayedMusic;
        private static string _currentPlayingMusic;

        public static string PlayLastMusic()
        {
            return PlayMusic(_lastPlayedMusic);
        }

        public static string PlayMusic(string eventName, float fadeInTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return string.Empty;
            if (!MusicEnabled)
            {
                UpdateLastMusic(eventName);
                return string.Empty;
            }
            var music = AsAssetLoader.GetAudioEvent(eventName) as MusicContainer;
            if (!music)
            {
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Music, AudioAction.Play, trigger, eventName, source, "Music not loaded");
                return string.Empty;
            }
            var clipName = MusicTransport.Instance.SetMusicQueue(music, fadeInTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Play, trigger, eventName, source);
            UpdateLastMusic(eventName);
            return clipName;
        }

        private static void UpdateLastMusic(string musicName)
        {
            _lastPlayedMusic = string.IsNullOrEmpty(_currentPlayingMusic) ? musicName : _currentPlayingMusic;
            _currentPlayingMusic = musicName;
        }

        public static void StopMusic(float fadeOutTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.Stop(fadeOutTime);       
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Stop, trigger, _currentPlayingMusic, source, fadeOutTime + "s fade out");
            ResetMusic();
        }

        internal static void ResetMusic()
        {
            _lastPlayedMusic = _currentPlayingMusic;
            _currentPlayingMusic = string.Empty;
        }

        public static void PlayStinger(string stingerName)
        {
            if (string.IsNullOrEmpty(stingerName)) return;            
            var stinger = AsAssetLoader.GetAudioEvent(stingerName) as MusicStinger;          
            if (stinger)
                MusicTransport.Instance.QueueStinger(stinger);
        }

        public static void ActivateInstrument(string instrumentName, byte channel = 1)
        {
            if (string.IsNullOrEmpty(instrumentName))
                return;
            AsAssetLoader.LoadInstrument(instrumentName, channel);
        }
        
        public static void DeactivateInstrument(string instrumentName)
        {
            AsAssetLoader.UnloadInstrument(instrumentName);
        }
        #endregion

        #region Voice        
        private static string _currentPlayingVoice;
        public static string PlayVoice(string eventName, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!VoiceEnabled || string.IsNullOrEmpty(eventName)) return string.Empty;
            var voice = AsAssetLoader.GetAudioEvent(eventName) as VoiceEvent;
            if (!voice)
            {
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Voice, AudioAction.Play, trigger, eventName, soundSource, "Voice not loaded");
                return string.Empty;
            }
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = voice.Play(emitter, fadeInTime, endCallback);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Play, trigger, voice.name, soundSource);
            _currentPlayingVoice = eventName;
            return clipName;
        }

        public static void StopVoice(string eventName = null, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                if (string.IsNullOrEmpty(_currentPlayingVoice))
                {
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Voice, AudioAction.Stop, trigger, "N/A", soundSource, "No voice playing");
                    return;
                }
                eventName = _currentPlayingVoice;
            }

            var voice = AsAssetLoader.GetAudioEvent(eventName) as VoiceEvent;
            if (!voice)
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Voice, AudioAction.Stop, trigger, eventName, soundSource, "Voice not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                voice.Stop(emitter, fadeOutTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Stop, trigger, eventName, soundSource);
            }
        }
        #endregion       
        
        #region Controls
        public static void SetAudioMixerParameter(string parameterName, float targetValue, float fadeTime = 0f)
        {
            if (AudioMixer.GetFloat(parameterName, out var currentValue))
            {
                GlobalAudioEmitter.Instance.SetAudioMixerParameter(parameterName, currentValue, targetValue, fadeTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.AudioMixer, AudioAction.SetValue, AudioTriggerSource.Code, parameterName, null, "Set to " + targetValue);   
            }
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
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent) return;
            audioEvent.StopAll(fadeOutTime);
            AsUnityHelper.DebugToProfiler(Severity.Notification, audioEvent.GetEventType(), AudioAction.Stop, AudioTriggerSource.Code, eventName, null, "Stop All");
        }
        
        public static void MuteEvent(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.DebugToProfiler(Severity.Error, audioEvent.GetEventType(), AudioAction.Mute, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Mute(emitter, fadeOutTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, audioEvent.GetEventType(), AudioAction.Mute, trigger, eventName, soundSource);
            }
        }
        
        public static void UnMuteEvent(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.DebugToProfiler(Severity.Error, audioEvent.GetEventType(), AudioAction.UnMute, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.UnMute(emitter, fadeInTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, audioEvent.GetEventType(), AudioAction.UnMute, trigger, eventName, soundSource);
            }
        }
        
        public static void PauseEvent(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.DebugToProfiler(Severity.Error, audioEvent.GetEventType(), AudioAction.Pause, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Pause(emitter, fadeOutTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, audioEvent.GetEventType(), AudioAction.Pause, trigger, eventName, soundSource);
            }
        }
        
        public static void ResumeEvent(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.DebugToProfiler(Severity.Error, audioEvent.GetEventType(), AudioAction.Resume, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Resume(emitter, fadeInTime);
                AsUnityHelper.DebugToProfiler(Severity.Notification, audioEvent.GetEventType(), AudioAction.Resume, trigger, eventName, soundSource);
            }
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
            AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Switch, AudioAction.GetValue, trigger, switchGroupName, affectedGameObject, "Switch not found");   	
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
                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Parameter, AudioAction.SetValue, trigger, parameterName, affectedGameObject, "Parameter not loaded");            
        }

        public static float GetParameterValue(string parameterName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(parameterName)) return 0f;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                return ap.GetValue(affectedGameObject);         
            AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Parameter, AudioAction.GetValue, AudioTriggerSource.Code, parameterName, affectedGameObject, "Parameter not loaded");	
            return 0f;
        }
        #endregion
		
        #region SoundBank
        public static void LoadBank(string bankName, Action onLoadFinished = null, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!string.IsNullOrEmpty(bankName)) 
                BankManager.LoadBank(bankName, onLoadFinished, source, trigger);
        }

        public static void UnloadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!string.IsNullOrEmpty(bankName)) 
                BankManager.UnloadBank(bankName, source, trigger);
        }  
        
        public static void UnloadBanks(string nameFilter)
        {
            BankManager.UnloadBanks(nameFilter);                                                   
        }   
        #endregion
                
        #region PreferenceSetting
        public static bool DisableAudio;
        internal static AudioMixer AudioMixer;

        public static void LoadPreferenceSettings()
        {
            SoundEnabled = SoundEnabled;
            MusicEnabled = MusicEnabled;
            VoiceEnabled = VoiceEnabled;
            SoundVolume = SoundVolume;
            MusicVolume = MusicVolume;
            VoiceVolume = VoiceVolume;
        }

        internal static AudioMixerGroup GetAudioMixer(string type, string subMixer = "")
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
                if (SoundEnabled == value) return;
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
                if (VoiceEnabled == value) return;
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
                if (MusicEnabled == value) return;
                var currentStatus = MusicEnabled;
                var volume = value ? MusicVolume : 0f;
                var decibel = LinearToDecibel(volume);
                AudioMixer.SetFloat("MusicVolume", decibel);
                PlayerPrefs.SetInt("AUDIO_MUSIC", value ? 1: 0);             
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Music On" : "Music Off");
                if (!value && currentStatus)
                    MusicTransport.Instance.Stop();
                if (value && !currentStatus)
                    PlayMusic(_currentPlayingMusic);
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
    }			
}