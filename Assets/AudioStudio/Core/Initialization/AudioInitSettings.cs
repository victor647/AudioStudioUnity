using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio
{       
    [CreateAssetMenu(fileName = "AudioInitSettings", menuName = "AudioStudio/Audio Init Settings")]  
    public class AudioInitSettings : ScriptableObject
    {
        private static AudioInitSettings _instance;
        public static AudioInitSettings Instance
        {
            get
            {
                if (!_instance)
                    _instance = AsUnityHelper.GetOrCreateAsset<AudioInitSettings>("Assets/" + AudioPathSettings.AudioStudioLibraryPath + "/Configs/AudioInitSettings.asset");
                return _instance;
            }
            set => _instance = value;
        }

        public static bool Initialized;
        public bool DisableAudio;
        public bool UseMicrophone;
        public bool UseMidi;
        public AudioMixer AudioMixer;
        public AudioPathSettings PathSettings;
        public Severity DebugLogLevel = Severity.Error;

        public void Initialize(bool loadAudioData = false)
        {
            if (Initialized) return;
            AudioManager.DisableAudio = DisableAudio;
            if (DisableAudio) return;
            
            Initialized = true;
            
            AsUnityHelper.DebugLogLevel = DebugLogLevel;
            AudioPathSettings.Instance = PathSettings;
            AudioManager.AudioMixer = AudioMixer;
            CreateGlobalAudioEmitter();
            LoadVolumeSettings();
            if (loadAudioData)
                AsAssetLoader.LoadAudioInitData();
        }

        private void CreateGlobalAudioEmitter()
        {
            var gae = new GameObject("Global Audio Emitter");
            gae.AddComponent<GlobalAudioEmitter>();
            gae.AddComponent<AudioListener>();
            if (UseMicrophone)
                GlobalAudioEmitter.AddMicrophone();
            if (UseMidi)
                GlobalAudioEmitter.AddMidi();
        }

        private static void LoadVolumeSettings()
        {
            AudioManager.SoundEnabled = AudioManager.SoundEnabled;
            AudioManager.MusicEnabled = AudioManager.MusicEnabled;
            AudioManager.VoiceEnabled = AudioManager.VoiceEnabled;
            AudioManager.SoundVolume = AudioManager.SoundVolume;
            AudioManager.MusicVolume = AudioManager.MusicVolume;
            AudioManager.VoiceVolume = AudioManager.VoiceVolume;
        }
    }   
}