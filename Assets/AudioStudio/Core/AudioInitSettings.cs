using AudioStudio.Components;
using AudioStudio.Configs;
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
        public bool UseMicrophone;
        public bool UseMidi;
        public SoundBankReference[] StartBanks = new SoundBankReference[0];
        public PostEventReference[] StartEvents = new PostEventReference[0];                
        public bool PostEvents;
        public bool LoadBanks;        
        public AudioMixer AudioMixer;
        public AudioPathSettings PathSettings;

        public void Initialize()
        {
            if (Initialized) return;     
            Initialized = true;
            AudioPathSettings.Instance = PathSettings;
            InitAudioManager();
            CreateGlobalAudioEmitter();
            ListenerManager.Init();
            AsAssetLoader.Init();                                                           
            if (LoadBanks) LoadInitBanks();
            AudioManager.LoadPreferenceSettings();
            if (PostEvents) PostGameStartEvents();                        
        }

        public void InitializeWithoutLoading()
        {
            if (Initialized) return;   
            Initialized = true;
            InitAudioManager();
            CreateGlobalAudioEmitter();
            ListenerManager.Init();
            AsAssetLoader.Init();                                                                       
            AudioManager.LoadPreferenceSettings();                        
        }

        private void InitAudioManager()
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            AudioManager.Platform = Platform.PC;
#else 
            AudioManager.Platform = Platform.Web;
#endif
            AudioManager.AudioMixer = AudioMixer;
        }

        private void CreateGlobalAudioEmitter()
        {
            var gae = new GameObject("Global Audio Emitter");
            gae.AddComponent<GlobalAudioEmitter>();
            if (UseMicrophone)
                GlobalAudioEmitter.AddMicrophone();
            if (UseMidi)
                GlobalAudioEmitter.AddMidi();
        }

        private void LoadInitBanks()
        {
            foreach (var bank in StartBanks)
            {
                bank.Load(null, null, AudioTriggerSource.Initialization);
            }        
        }
        
        private void PostGameStartEvents()
        {
            foreach (var evt in StartEvents)
            {
                evt.Post(null, AudioTriggerSource.Initialization);
            }                                    
        }
    }   
}