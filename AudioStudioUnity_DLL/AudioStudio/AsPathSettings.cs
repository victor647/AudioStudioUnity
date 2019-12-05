using System.IO;
using UnityEngine;

namespace AudioStudio
{
    public static class AsPathSettings
    {
        public const string AudioStudioLibraryPath = "Resources/Audio"; 
        public static string AudioStudioLibraryPathFull => Path.Combine(Application.dataPath, AudioStudioLibraryPath);
        public static string EditorConfigPathFull => Path.Combine(AudioStudioLibraryPathFull, "Configs/Editor");
        public const string AudioStudioPluginPath = "Plugins/ThirdParty/AudioStudio"; 
        public static string AudioStudioPluginPathFull => Path.Combine(Application.dataPath, AudioStudioPluginPath);

        public const string OriginalsPath = "Resources/Audio/Originals";
        public const string SoundEventsPath = "Resources/Audio/Events/Sound";
        public const string SoundBanksPath = "Resources/Audio/SoundBanks";
        public const string MusicInstrumentsPath = "Resources/Audio/Instruments";
        public const string MusicEventsPath = "Resources/Audio/Events/Music";
        public const string VoiceEventsPath = "Resources/Audio/Events/Voice";
        public const string StreamingClipsPath = "Resources/Audio/WebGL/StreamingClips";
        public const string WebEventsPath = "Resources/Audio/WebGL/Events";
    } 
}