using System.IO;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
    [CreateAssetMenu(fileName = "AudioPathSettings", menuName = "AudioStudio/Path Settings")]
    public partial class AudioPathSettings : ScriptableObject
    {
        private static AudioPathSettings _instance;

        public static AudioPathSettings Instance
        {
            get
            {
                if (!_instance)
                {
                    var loadPath = "Assets/" + AudioStudioLibraryPath + "/Configs/AudioPathSettings.asset";
                    _instance = AsUnityHelper.GetOrCreateAsset<AudioPathSettings>(loadPath);
                }
                return _instance;
            }
            set => _instance = value;
        }
        
        public string AudioResourcesPath = "Resources/Audio";
        public string EventsPath => Path.Combine(AudioResourcesPath, "Events");
        public string OriginalsPath => Path.Combine(AudioResourcesPath, "Originals");
        public string SoundEventsPath => Path.Combine(EventsPath, "SFX");
        public string MusicEventsPath => Path.Combine(EventsPath, "Music");
        public string VoiceEventsPath => Path.Combine(EventsPath, "Voice");
        public string BuildAssetsPath = "Resources/Audio";
        public string SoundBanksPath => Path.Combine(BuildAssetsPath, "SoundBanks");
        public string MusicInstrumentsPath => Path.Combine(BuildAssetsPath, "Instruments");
        public string StartScenePath;
        
        [Range(10, 100)]
        public int MusicQuality = 50;
        [Range(10, 100)]
        public int SoundQuality = 40;
        [Range(10, 100)]
        public int VoiceQuality = 30;
        public int StreamDurationThreshold = 5;
        public static string EditorConfigPath => AudioStudioLibraryPath + "/Configs/Editor";
        
        public static string AudioStudioLibraryPathFull => Path.Combine(Application.dataPath, AudioStudioLibraryPath);
        public static string AudioStudioPluginPathFull => Path.Combine(Application.dataPath, AudioStudioPluginPath);
        public static string EditorConfigPathFull 
        {
            get 
            {
                var path = Path.Combine(Application.dataPath, EditorConfigPath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }
    }
}