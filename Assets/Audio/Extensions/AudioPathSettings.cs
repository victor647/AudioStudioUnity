using System.IO;
using UnityEngine;

namespace AudioStudio
{
    public class AudioPathSettings
    {
        public const string AudioStudioLibraryPath = "Audio"; 
        public static string AudioStudioLibraryPathFull => Path.Combine(Application.dataPath, AudioStudioLibraryPath);
    
        public const string OriginalsPath = "Resources/Audio/Originals";
        public const string ControllersPath = "Resources/Audio/Controllers";
        public const string SoundEventsPath = "Resources/Audio/SoundEvents";
        public const string SoundBanksPath = "Resources/Audio/SoundBanks";
        public const string MusicInstrumentsPath = "Resources/Audio/Instruments";
        public const string MusicEventsPath = "Resources/Audio/Events/Music";
        public const string VoiceEventsPath = "Resources/Audio/Events/Voice";
        public const string StreamingClipsPath = "Resources/Audio/WebGL/StreamingClips";
        public const string WebEventsPath = "Resources/Audio/WebGL/Events";
    } 
}