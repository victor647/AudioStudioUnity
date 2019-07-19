using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio
{       
    [CreateAssetMenu(fileName = "AudioInitSettings", menuName = "Audio/Audio Init Settings")]  
    public class AudioInitSettings : ScriptableObject
    {
        private static AudioInitSettings _instance;
        public static AudioInitSettings Instance
        {
            get
            {
                if (!_instance)
                {
#if UNITY_EDITOR
                    var loadPath = "Assets/" + AudioPathSettings.AudioStudioLibraryPath + "/Configs/AudioInitSettings.asset";
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioInitSettings>(loadPath);
                    if (!_instance)
                    {
                        _instance = CreateInstance<AudioInitSettings>();
                        UnityEditor.AssetDatabase.CreateAsset(_instance, loadPath);
                    }
#else
                    _instance = CreateInstance<AudioInitSettings>();
#endif                    
                }
                return _instance;
            }
            set { _instance = value; }
        }


        public static bool Initialized;        
        [EnumFlag(typeof(MessageType))]
        public MessageType ProfilerLogLevel = MessageType.Error;
        public SoundBankReference[] StartBanks = new SoundBankReference[0];
        public AudioEventReference[] StartEvents = new AudioEventReference[0];                
        public bool PostEvents;
        public bool LoadBanks;        
        public AudioMixer AudioMixer;    
        public SpatialSetting DefaultSpatialSetting;
        
        public void Initialize()
        {
            if (Initialized) return;     
            Initialized = true;
            InitAudioManager();
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.AudioInit, AudioAction.Activate, "Initialization");  
            AudioManager.DebugToProfiler(MessageType.Notification, ObjectType.Voice, AudioAction.SetValue, "Voice Language", "Global", AudioManager.VoiceLanguage.ToString());
            AudioAssetLoader.Init();                                                           
            if (LoadBanks) LoadInitBanks();
            AudioManager.LoadPreferenceSettings();
            if (PostEvents) PostGameStartEvents();                        
        }

        public void InitializeWithoutObjects()
        {
            if (Initialized) return;   
            Initialized = true;
            InitAudioManager();                          
            AudioAssetLoader.Init();                                                                       
            AudioManager.LoadPreferenceSettings();                        
        }

        private void InitAudioManager()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            AudioManager.Platform = Platform.PC;   
#elif UNITY_IOS || UNITY_ANDROID
            AudioManager.Platform = Platform.Mobile;
#elif UNITY_WEBGL
            AudioManager.Platform = Platform.Web;       
#endif   
            AudioManager.LogLevel = ProfilerLogLevel;            
            AudioManager.AudioMixer = AudioMixer;
            AudioManager.DefaultSpatialSetting = DefaultSpatialSetting;
            SetListener();
        }
        
        private static void SetListener()
        {   
            var audioListener = new GameObject("Audio Listener");
            var listener = FindObjectOfType<AudioListener>();
            
            //replace the existing listener with the new one
            if (listener)
            {
                AudioManager.SyncTransformWithParent(audioListener, listener.gameObject);
                Destroy(listener);                
            }
            else //add listener to main camera             
            {
                var camera = Camera.main;
                if (camera != null)                
                    AudioManager.SyncTransformWithParent(audioListener, camera.gameObject);                                                                                                                             
            }             
            AudioManager.AudioListener = audioListener.AddComponent<AudioListener>();
        }
        
        private void LoadInitBanks()
        {
            foreach (var bank in StartBanks)
            {
                bank.Load();
            }        
        }
        
        private void PostGameStartEvents()
        {
            foreach (var evt in StartEvents)
            {
                evt.Post();
            }                                    
        }
    }   
}