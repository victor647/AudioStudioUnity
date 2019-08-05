using UnityEngine;
using System;
using AudioStudio.Components;
using AudioStudio.Configs;
using UnityEngine.Audio;

namespace AudioStudio
{		
    public static partial class AudioManager
    {		        
        public static Platform Platform;
        public static AudioListener AudioListener;        			
        
        #region Sound           
        public static void PlaySound(string eventName, GameObject soundSource = null, float fadeInTime = -1f, Action<GameObject> callback = null)
        {            
            if (string.IsNullOrEmpty(eventName)) return;   
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var sc = AudioAssetLoader.GetSoundEvent(eventName);
            if (sc)
                sc.PostEvent(soundSource, fadeInTime, callback);                                       
            else                            
                DebugToProfiler(ProfilerMessageType.Error, ObjectType.Sound, AudioAction.PostEvent, eventName, soundSource.name, "Event not found");                         
        }

        public static void StopSound(string eventName, GameObject soundSource = null, float fadeOutTime = -1f)
        {
            if (string.IsNullOrEmpty(eventName)) return;      
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var sc = AudioAssetLoader.GetSoundEvent(eventName);
            if (sc)
            {
                sc.Stop(soundSource, fadeOutTime);     
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.StopEvent, eventName, soundSource.name);
            }
            else                           
                DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Sound, AudioAction.StopEvent, eventName, soundSource.name, "Event not found");            
        }

        public static void StopAll(GameObject soundSource = null, float fadeOutTime = 0f)
        {
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var instances = soundSource.GetComponentsInChildren<AudioEventInstance>();
            if (instances.Length > 0)
            {
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.StopEvent, "Stop All", soundSource.name);
                foreach (var sci in instances)
                {
                    sci.Stop(fadeOutTime);
                }
            }
            else
                DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Sound, AudioAction.StopEvent, "Stop All", "No playing instance found");
        }
        
        public static void StopAll(string eventName, float fadeOutTime = 0f)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sc = AudioAssetLoader.GetSoundEvent(eventName);
            if (sc)
                sc.StopAll(fadeOutTime);  			
            else                           
                DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Sound, AudioAction.StopEvent, eventName, "Global", "SoundEvent does not exist, stop fails");            
        }
        #endregion

        #region Music		
        private static string _lastPlayedMusic;
        private static string _currentPlayingMusic;

        public static void PlayLastMusic()
        {
            PlayMusic(_lastPlayedMusic);
        }
        
        public static void PlayMusic(string eventName, float fadeInTime = -1f, float fadeOutTime = -1f, float exitOffset = -1f, float entryOffset = -1f)
        {
            if (string.IsNullOrEmpty(eventName) || eventName == _currentPlayingMusic) return;                

#if UNITY_EDITOR || !UNITY_WEBGL
            var music = AudioAssetLoader.LoadMusic(eventName);          
            if (music)
                MusicTransport.Instance.SetMusicQueue(music, fadeInTime, fadeOutTime, exitOffset, entryOffset);                      
#else
            var music = AudioAssetLoader.LoadMusicWeb(eventName);
	        if (music != null)
		        WebMusicPlayer.Instance.PlayMusic(music);
#endif
            _lastPlayedMusic = string.IsNullOrEmpty(_currentPlayingMusic) ? eventName : _currentPlayingMusic;
            _currentPlayingMusic = eventName;
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
            var stinger = AudioAssetLoader.LoadStinger(stingerName);          
            if (stinger)
                MusicTransport.Instance.QueueStinger(stinger);             
#endif               
        }

        public static void PlayInstrument(string instrumentName, byte channel = 1)
        {
            AudioAssetLoader.LoadInstrument(instrumentName, channel);
        }
        
        public static void StopInstrument(string instrumentName)
        {
            AudioAssetLoader.UnloadInstrument(instrumentName);
        }
        #endregion

        #region Voice        
        private static string _currentVoiceEvent;
        public static void PlayVoice(string eventName, GameObject soundSource = null, float fadeInTime = -1f, Action<GameObject> endCallback = null)
        {
            if (!VoiceEnabled || string.IsNullOrEmpty(eventName))  return;
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
#if UNITY_EDITOR || !UNITY_WEBGL
            var voice = AudioAssetLoader.LoadVoice(eventName);   
#else
            var voice = AudioAssetLoader.LoadVoiceWeb(eventName);
#endif
            if (voice)
                voice.PostEvent(soundSource, fadeInTime, endCallback);            
            _currentVoiceEvent = eventName;
        }
		
        public static void StopVoice(string eventName, GameObject soundSource = null, float fadeOutTime = -1f)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var voice = AudioAssetLoader.LoadVoice(eventName);          
            if (voice)
                voice.Stop(soundSource, fadeOutTime);   
            else                        
                DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Voice, AudioAction.StopEvent, eventName, soundSource.name, "VoiceEvent does not exist, stop fails");                           
        }

        public static void StopCurrentVoice()
        {
            StopVoice(_currentVoiceEvent);
        }
        #endregion       
		
        #region Switch		
        public static void SetSwitchGlobal(string switchGroupName, string switchName)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;
            var swc = AudioAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitchGlobal(switchName);
            else                            
                DebugToProfiler(ProfilerMessageType.Error, ObjectType.Switch, AudioAction.SetValue, switchGroupName, "Global", "Switch not found");                                    
        }

        public static void SetSwitch(string switchGroupName, string switchName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;  
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AudioAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitch(switchName, affectedGameObject);
            else                            
                DebugToProfiler(ProfilerMessageType.Error, ObjectType.Switch, AudioAction.SetValue, switchGroupName, "Global", "Switch not found");                                                               
        }

        public static string GetSwitch(string switchGroupName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(switchGroupName)) return null;      
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AudioAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                return swc.GetSwitch(affectedGameObject);                                
            DebugToProfiler(ProfilerMessageType.Error, ObjectType.Switch, AudioAction.GetValue, switchGroupName, "Global", "Switch not found");   	
            return "";
        }
        #endregion
	
        #region Parameter				
        public static void SetParameterValueGlobal(string parameterName, float parameterValue)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            var ap = AudioAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValueGlobal(parameterValue);   
            else
                DebugToProfiler(ProfilerMessageType.Error, ObjectType.Parameter, AudioAction.SetValue, parameterName, "Global", "Parameter not found");                                    
        }

        public static void SetParameterValue(string parameterName, float parameterValue, GameObject affectedGameObject = null)
        {                        
            if (string.IsNullOrEmpty(parameterName)) return;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AudioAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValue(parameterValue, affectedGameObject);   
            else           
                DebugToProfiler(ProfilerMessageType.Error, ObjectType.Parameter, AudioAction.SetValue, parameterName, "Global", "Parameter not found");            
        }

        public static float GetParameterValue(string parameterName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(parameterName)) return 0f;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AudioAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                return ap.GetValue(affectedGameObject);         
            DebugToProfiler(ProfilerMessageType.Error, ObjectType.Parameter, AudioAction.GetValue, parameterName, "Global", "Parameter not found");	
            return 0f;
        }
        #endregion
		
        #region SoundBank
        public static void LoadBank(string bankName)
        {   
            AudioAssetLoader.LoadBank(bankName);         
        }

        public static void UnloadBank(string bankName)
        {
            AudioAssetLoader.UnloadBank(bankName);                                                   
        }  
        
        public static void UnloadBanks(string nameFilter)
        {
            AudioAssetLoader.UnloadBanks(nameFilter);                                                   
        }   
        #endregion
                
        #region PreferenceSetting
        public static AudioMixer AudioMixer;
        public static SpatialSetting DefaultSpatialSetting;
        
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
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, value ? "Sound On" : "Sound Off", "Global");
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
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, value ? "Voice On" : "Voice Off", "Global");
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
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Music, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, value ? "Music On" : "Music Off", "Global");
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
                DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.SetValue, "Set Language", "Global", value.ToString());
            }
        }        
        #endregion
        
        #region Listener
        public static void MoveListenerGameObject(GameObject newGameObject)
        {
            var go = AudioListener.gameObject;
            go.transform.position = newGameObject.transform.position;
            go.transform.parent = newGameObject.transform;
        }

        public static void MoveListener(Vector3 position)
        {
            var go = AudioListener.gameObject;
            go.transform.Translate(position);            
        }
        
        public static void RotateListener(Vector3 rotation)
        {
            var go = AudioListener.gameObject;            
            go.transform.Rotate(rotation);
        }
        
        public static void ResetListenerTransform()
        {
            var go = AudioListener.gameObject;
            SyncTransformWithParent(go);
        }

        public static void SyncTransformWithParent(GameObject source, GameObject parent = null)
        {
            var parentTransform = parent ? parent.transform : source.transform.parent;
            if (parent)
                source.transform.parent = parentTransform;                                       
            source.transform.position = parentTransform.position;
            source.transform.rotation = parentTransform.rotation;
        }
        #endregion
        
        #region Profiler
        public static void DebugToProfiler(ProfilerMessageType messageType, ObjectType objectType, AudioAction action, string eventName, string gameObject = "Global Audio Emitter", string message = "")
        {
#if UNITY_EDITOR
            if (AudioProfiler.Instance)
                AudioProfiler.Instance.AddLog(messageType, objectType, action, eventName, gameObject, message, Time.time.ToString("0.000"));
#else
            if (messageType == MessageType.Error && Debug.unityLogger.logEnabled)
            {
                var log = $"AudioManager: {messageType}_{objectType}_{action}\tName: {eventName}\tGameObject: {gameObject}\tMessage: {message}";
                Debug.Log(log);
            }
#endif
        }
        #endregion
    }			
}