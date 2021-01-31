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
        /// <summary>
        /// Post a simple sound effect event by name with optional fade in and end callback.
        /// </summary>
        public static string PlaySound(string eventName, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (string.IsNullOrEmpty(eventName) || !SoundEnabled) return string.Empty;
            var sound = AsAssetLoader.GetAudioEvent(eventName) as SoundContainer;
            if (!sound)
            {
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.SFX, AudioAction.Play, trigger, eventName, soundSource, "SFX not loaded");
                return string.Empty;
            }
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = sound.Play(emitter, fadeInTime, endCallback);
            if (!string.IsNullOrEmpty(clipName))
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, trigger, eventName, soundSource);
            return clipName;
        }
        
        /// <summary>
        /// Post a simple sound effect event with optional fade in and end callback.
        /// </summary>
        public static string PlaySound(SoundContainer sound, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (!SoundEnabled || !sound) return string.Empty;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = sound.Play(emitter, fadeInTime, endCallback);
            if (!string.IsNullOrEmpty(clipName))
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, trigger, sound.name, soundSource);
            return clipName;
        }

        /// <summary>
        /// Stop a sound currently playing by name with fade out.
        /// </summary>
        public static void StopSound(string eventName, GameObject soundSource = null, float fadeOutTime = 0.2f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var sound = AsAssetLoader.GetAudioEvent(eventName) as SoundContainer;
            if (!sound)
                AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.SFX, AudioAction.Stop, trigger, eventName, soundSource, "SFX not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                sound.Stop(emitter, fadeOutTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, trigger, eventName, soundSource);
            }
        }
        
        /// <summary>
        /// Stop a sound currently playing with fade out.
        /// </summary>
        public static void StopSound(SoundContainer sound, GameObject soundSource = null, float fadeOutTime = 0.2f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!sound) return;
            var emitter = ValidateSoundSource(soundSource, trigger);
            sound.Stop(emitter, fadeOutTime);
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, trigger, sound.name, soundSource);
        }
        #endregion
        
        #region Music		
        private static string _lastPlayedMusic;
        private static string _currentPlayingMusic;

        /// <summary>
        /// Switch back to the last music played.
        /// </summary>
        public static string PlayLastMusic()
        {
            return PlayMusic(_lastPlayedMusic);
        }

        /// <summary>
        /// Post a background music event by name, ignore if same event is already playing.
        /// </summary>
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
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Music, AudioAction.Play, trigger, eventName, source, "Music not loaded");
                return string.Empty;
            }
            var clipName = MusicTransport.Instance.SetMusicQueue(music, fadeInTime);
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Music, AudioAction.Play, trigger, eventName, source);
            UpdateLastMusic(eventName);
            return clipName;
        }
        
        /// <summary>
        /// Post a background music event, ignore if same event is already playing.
        /// </summary>
        public static string PlayMusic(MusicContainer music, float fadeInTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!music) return string.Empty;
            if (!MusicEnabled)
            {
                UpdateLastMusic(music.name);
                return string.Empty;
            }
            var clipName = MusicTransport.Instance.SetMusicQueue(music, fadeInTime);
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Music, AudioAction.Play, trigger, music.name, source);
            UpdateLastMusic(music.name);
            return clipName;
        }
        
        /// <summary>
        /// Stop the background music currently playing.
        /// </summary>
        public static void StopMusic(float fadeOutTime = 0f, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            MusicTransport.Instance.Stop(fadeOutTime);       
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Music, AudioAction.Stop, trigger, _currentPlayingMusic, source, fadeOutTime + "s fade out");
            ResetMusic();
        }

        private static void UpdateLastMusic(string musicName)
        {
            _lastPlayedMusic = string.IsNullOrEmpty(_currentPlayingMusic) ? musicName : _currentPlayingMusic;
            _currentPlayingMusic = musicName;
        }

        // reset music playback history when music stops
        internal static void ResetMusic()
        {
            _lastPlayedMusic = _currentPlayingMusic;
            _currentPlayingMusic = string.Empty;
        }

        /// <summary>
        /// Play a stinger on top of current playing background music.
        /// </summary>
        public static void PlayStinger(string stingerName)
        {
            if (string.IsNullOrEmpty(stingerName)) return;            
            var stinger = AsAssetLoader.GetAudioEvent(stingerName) as MusicStinger;          
            if (stinger)
                MusicTransport.Instance.QueueStinger(stinger);
        }

        /// <summary>
        /// Activate a MIDI instrument.
        /// </summary>
        public static void ActivateInstrument(string instrumentName, byte channel = 1)
        {
            if (string.IsNullOrEmpty(instrumentName))
                return;
            AsAssetLoader.LoadInstrument(instrumentName, channel);
        }
        
        /// <summary>
        /// Deactivate a MIDI instrument.
        /// </summary>
        public static void DeactivateInstrument(string instrumentName)
        {
            AsAssetLoader.UnloadInstrument(instrumentName);
        }
        #endregion

        #region Voice        
        private static string _currentPlayingVoice;
        
        /// <summary>
        /// Post a voice dialog event by name with optional fade in and end callback.
        /// </summary>
        public static string PlayVoice(string eventName, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!VoiceEnabled || string.IsNullOrEmpty(eventName)) return string.Empty;
            var voice = AsAssetLoader.GetAudioEvent(eventName) as VoiceEvent;
            if (!voice)
            {
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Voice, AudioAction.Play, trigger, eventName, soundSource, "Voice not loaded");
                return string.Empty;
            }
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = voice.Play(emitter, fadeInTime, endCallback);
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Voice, AudioAction.Play, trigger, voice.name, soundSource);
            _currentPlayingVoice = eventName;
            return clipName;
        }
        
        /// <summary>
        /// Post a voice dialog event with optional fade in and end callback.
        /// </summary>
        public static string PlayVoice(VoiceEvent voice, GameObject soundSource = null, float fadeInTime = 0f, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!VoiceEnabled || !voice) return string.Empty;
            var emitter = ValidateSoundSource(soundSource, trigger);
            var clipName = voice.Play(emitter, fadeInTime, endCallback);
            AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Voice, AudioAction.Play, trigger, voice.name, soundSource);
            _currentPlayingVoice = voice.name;
            return clipName;
        }

        /// <summary>
        /// Stop a voice dialog event from playing with fade out. Leave event name empty if stopping the current playing event.
        /// </summary>
        public static void StopVoice(string eventName = null, GameObject soundSource = null, float fadeOutTime = 0.2f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                if (string.IsNullOrEmpty(_currentPlayingVoice))
                {
                    AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.Voice, AudioAction.Stop, trigger, "N/A", soundSource, "No voice playing");
                    return;
                }
                eventName = _currentPlayingVoice;
            }

            var voice = AsAssetLoader.GetAudioEvent(eventName) as VoiceEvent;
            if (!voice)
                AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.Voice, AudioAction.Stop, trigger, eventName, soundSource, "Voice not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                voice.Stop(emitter, fadeOutTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Voice, AudioAction.Stop, trigger, eventName, soundSource);
            }
        }
        
        /// <summary>
        /// Stop a voice dialog event from playing with fade out. Leave event empty if stopping the current playing event.
        /// </summary>
        public static void StopVoice(VoiceEvent voice = null, GameObject soundSource = null, float fadeOutTime = 0.2f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!voice)
                StopVoice(_currentPlayingVoice, soundSource, fadeOutTime, trigger);
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                voice.Stop(emitter, fadeOutTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Voice, AudioAction.Stop, trigger, voice.name, soundSource);
            }
        }
        #endregion       
        
        #region Controls
        /// <summary>
        /// Set an exposed parameter from the audio mixer.
        /// </summary>
        public static void SetAudioMixerParameter(string parameterName, float targetValue, float fadeTime = 0f)
        {
            if (AudioMixer.GetFloat(parameterName, out var currentValue))
            {
                GlobalAudioEmitter.Instance.SetAudioMixerParameter(parameterName, currentValue, targetValue, fadeTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.AudioMixer, AudioAction.SetValue, AudioTriggerSource.Code, parameterName, null, "Set to " + targetValue);   
            }
        }
        
        /// <summary>
        /// Stop all sounds/music/voices from playing on a game object with fade out.
        /// </summary>
        public static void StopAll(GameObject soundSource = null, float fadeOutTime = 0.2f)
        {
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            var instances = soundSource.GetComponentsInChildren<AudioEventInstance>();
            if (instances.Length > 0)
            {
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, AudioAction.Stop, AudioTriggerSource.Code, "Stop All", soundSource);
                foreach (var sci in instances)
                {
                    sci.Stop(fadeOutTime);
                }
            }
            else
                AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.SFX, AudioAction.Stop, AudioTriggerSource.Code, "Stop All", soundSource, "No playing instance found");
        }
        
        /// <summary>
        /// Stop all instances of an AudioEvent with fade out.
        /// </summary>
        public static void StopAll(string eventName, float fadeOutTime = 0.2f)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent) return;
            audioEvent.StopAll(fadeOutTime);
            AsUnityHelper.AddLogEntry(Severity.Notification, audioEvent.GetEventType(), AudioAction.Stop, AudioTriggerSource.Code, eventName, null, "Stop All");
        }
        
        /// <summary>
        /// Mute an AudioEvent on a game object with optional fade out.
        /// </summary>
        public static void MuteEvent(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.AddLogEntry(Severity.Error, audioEvent.GetEventType(), AudioAction.Mute, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Mute(emitter, fadeOutTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, audioEvent.GetEventType(), AudioAction.Mute, trigger, eventName, soundSource);
            }
        }
        
        /// <summary>
        /// Unmute an AudioEvent on a game object with optional fade in.
        /// </summary>
        public static void UnMuteEvent(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.AddLogEntry(Severity.Error, audioEvent.GetEventType(), AudioAction.UnMute, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.UnMute(emitter, fadeInTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, audioEvent.GetEventType(), AudioAction.UnMute, trigger, eventName, soundSource);
            }
        }
        
        /// <summary>
        /// Pause an AudioEvent on a game object with optional fade out.
        /// </summary>
        public static void PauseEvent(string eventName, GameObject soundSource = null, float fadeOutTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.AddLogEntry(Severity.Error, audioEvent.GetEventType(), AudioAction.Pause, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Pause(emitter, fadeOutTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, audioEvent.GetEventType(), AudioAction.Pause, trigger, eventName, soundSource);
            }
        }
        
        /// <summary>
        /// Resume an AudioEvent on a game object with optional fade in.
        /// </summary>
        public static void ResumeEvent(string eventName, GameObject soundSource = null, float fadeInTime = 0f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var audioEvent = AsAssetLoader.GetAudioEvent(eventName);
            if (!audioEvent)
                AsUnityHelper.AddLogEntry(Severity.Error, audioEvent.GetEventType(), AudioAction.Resume, trigger, eventName, soundSource, "Event not loaded");
            else
            {
                var emitter = ValidateSoundSource(soundSource, trigger);
                audioEvent.Resume(emitter, fadeInTime);
                AsUnityHelper.AddLogEntry(Severity.Notification, audioEvent.GetEventType(), AudioAction.Resume, trigger, eventName, soundSource);
            }
        }
        
        // check if the sound should be played by GlobalAudioEmitter or its trigger source
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
        /// <summary>
        /// Set an AudioSwitch value for all instances.
        /// </summary>
        public static void SetSwitchGlobal(string switchGroupName, string switchName, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitchGlobal(switchName);
            else                            
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, switchGroupName, null, "Switch not found");                                    
        }

        /// <summary>
        /// Set an AudioSwitch value on a game object.
        /// </summary>
        public static void SetSwitch(string switchGroupName, string switchName, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName) || string.IsNullOrEmpty(switchName)) return;  
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                swc.SetSwitch(switchName, affectedGameObject);
            else                            
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, switchGroupName, affectedGameObject, "Switch not found");                                                               
        }

        /// <summary>
        /// Get an AudioSwitch value from a game object.
        /// </summary>
        public static string GetSwitch(string switchGroupName, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(switchGroupName)) return null;      
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var swc = AsAssetLoader.GetAudioSwitch(switchGroupName);
            if (swc)
                return swc.GetSwitch(affectedGameObject);                                
            AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.Switch, AudioAction.GetValue, trigger, switchGroupName, affectedGameObject, "Switch not found");   	
            return "";
        }
        #endregion
	
        #region Parameter				
        /// <summary>
        /// Set an AudioParameter value for all instances.
        /// </summary>
        public static void SetParameterValueGlobal(string parameterName, float parameterValue, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValueGlobal(parameterValue);   
            else
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Parameter, AudioAction.SetValue, trigger, parameterName, null, "Parameter not found");                                    
        }

        /// <summary>
        /// Set an AudioParameter value on a game object.
        /// </summary>
        public static void SetParameterValue(string parameterName, float parameterValue, GameObject affectedGameObject = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {                        
            if (string.IsNullOrEmpty(parameterName)) return;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                ap.SetValue(parameterValue, affectedGameObject);   
            else           
                AsUnityHelper.AddLogEntry(Severity.Error, AudioObjectType.Parameter, AudioAction.SetValue, trigger, parameterName, affectedGameObject, "Parameter not loaded");            
        }

        /// <summary>
        /// Get an AudioParameter value from a game object.
        /// </summary>
        public static float GetParameterValue(string parameterName, GameObject affectedGameObject = null)
        {
            if (string.IsNullOrEmpty(parameterName)) return 0f;
            if (!affectedGameObject) affectedGameObject = GlobalAudioEmitter.GameObject;
            var ap = AsAssetLoader.GetAudioParameter(parameterName);
            if (ap)
                return ap.GetValue(affectedGameObject);         
            AsUnityHelper.AddLogEntry(Severity.Warning, AudioObjectType.Parameter, AudioAction.GetValue, AudioTriggerSource.Code, parameterName, affectedGameObject, "Parameter not loaded");	
            return 0f;
        }
        #endregion
		
        #region SoundBank
        /// <summary>
        /// Load a SoundBank by name with optional finish callback.
        /// </summary>
        public static void LoadBank(string bankName, Action onLoadFinished = null, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!string.IsNullOrEmpty(bankName)) 
                BankManager.LoadBank(bankName, onLoadFinished, source, trigger);
        }

        /// <summary>
        /// Unload a SoundBank by name.
        /// </summary>
        public static void UnloadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!string.IsNullOrEmpty(bankName)) 
                BankManager.UnloadBank(bankName, source, trigger);
        }  
        
        /// <summary>
        /// Unload all SoundBanks whose name contains a string.
        /// </summary>
        /// <param name="nameFilter"></param>
        public static void UnloadBanks(string nameFilter)
        {
            BankManager.UnloadBanks(nameFilter);                                                   
        }   
        #endregion
                
        #region PreferenceSetting
        /// <summary>
        /// Completely disable audio and all audio components to test performance without audio.
        /// </summary>
        public static bool DisableAudio;
        internal static AudioMixer AudioMixer;

        internal static AudioMixerGroup GetAudioMixer(string type, string subMixer = "")
        {
            return !AudioMixer ? null : AudioMixer.FindMatchingGroups("Master/" + type + (string.IsNullOrEmpty(subMixer) ? "" : "/" + subMixer))[0];
        }
        
        /// <summary>
        /// Turn off audio temporarily for purposes like playing video.
        /// </summary>
        public static void MuteAudio()
        {
            if (!AudioInitSettings.Initialized) return;
            if (SoundEnabled) 
                AudioMixer.SetFloat("SoundVolume", LinearToDecibel(0));
            if (VoiceEnabled) 
                AudioMixer.SetFloat("VoiceVolume", LinearToDecibel(0));
            if (MusicEnabled) 
                AudioMixer.SetFloat("MusicVolume", LinearToDecibel(0));
        }
        
        /// <summary>
        /// Put audio back for conditions like video finishes.
        /// </summary>
        public static void UnmuteAudio()
        {
            if (!AudioInitSettings.Initialized) return;
            if (SoundEnabled) 
                AudioMixer.SetFloat("SoundVolume", LinearToDecibel(SoundVolume));
            if (VoiceEnabled) 
                AudioMixer.SetFloat("VoiceVolume", LinearToDecibel(VoiceVolume));
            if (MusicEnabled) 
                AudioMixer.SetFloat("MusicVolume", LinearToDecibel(MusicVolume));
        }

        /// <summary>
        /// Turn on or off sound effects.
        /// </summary>
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
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.SFX, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Sound On" : "Sound Off");
            }
        }
        
        /// <summary>
        /// Turn on or off voice dialogs.
        /// </summary>
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
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Voice, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Voice On" : "Voice Off");
            }
        }
        
        /// <summary>
        /// Turn on or off background music.
        /// </summary>
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
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Music, 
                    value ? AudioAction.Activate : AudioAction.Deactivate, AudioTriggerSource.Code, value ? "Music On" : "Music Off");
                if (!value && currentStatus)
                    MusicTransport.Instance.Stop();
                if (value && !currentStatus)
                    PlayMusic(_currentPlayingMusic);
            }
        }

        /// <summary>
        /// Set volume of all sound effects, range from 0 to 100.
        /// </summary>
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

        /// <summary>
        /// Set volume of all voice dialogs, range from 0 to 100.
        /// </summary>
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
        
        /// <summary>
        /// Set volume of all background musics, range from 0 to 100.
        /// </summary>
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
        
        // convert linear volume to decibel value
        private static float LinearToDecibel(float linear)
        {
            return Mathf.Max(-80f, 20f * Mathf.Log10(linear) - 40f);
        }

        /// <summary>
        /// Change the language of all voice dialogs.
        /// </summary>
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
                AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Language, AudioAction.SetValue, AudioTriggerSource.Code, value.ToString());
            }
        }        
        #endregion
    }			
}