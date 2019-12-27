using System.Collections;
using System.Collections.Generic;
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
        
        public string OriginalResourcesPath = "Resources/Audio";
        public string SoundFilesPath => Path.Combine(OriginalResourcesPath, "Originals");
        public string SoundEventsPath => Path.Combine(OriginalResourcesPath, "Events/SFX");
        public string BuildAssetsPath = "Resources/Audio";
        public string MusicEventsPath => Path.Combine(BuildAssetsPath, "Events/Music");
        public string VoiceEventsPath => Path.Combine(BuildAssetsPath, "Events/Voice");
        public string SoundBanksPath => Path.Combine(BuildAssetsPath, "SoundBanks");
        public string MusicInstrumentsPath => Path.Combine(BuildAssetsPath, "Instruments");
        public string StreamingClipsPath => Path.Combine(BuildAssetsPath, "WebGL/StreamingClips");
        public string WebEventsPath => Path.Combine(BuildAssetsPath, "WebGL/Events");
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