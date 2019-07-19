#if UNITY_EDITOR
using System.IO;
using AudioStudio;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "AudioEditorSettings", menuName = "Audio/Audio Editor Settings")]
public class AudioEditorSettings : ScriptableObject
{        
    private static AudioEditorSettings _instance;
    public static AudioEditorSettings Instance
    {
        get
        {									
            if (!_instance)
            {
                const string loadPath = "Assets/" + AudioStudioLibraryPath + "/Editor/Configs/AudioEditorSettings.asset";
                _instance = AssetDatabase.LoadAssetAtPath<AudioEditorSettings>(loadPath);
            }						
            return _instance;
        }
    }

    //Setup the path for AudioStudio library folder here!
    public const string AudioStudioLibraryPath = "Audio";    
    public static string AudioStudioLibraryPathFull => Path.Combine(Application.dataPath, AudioStudioLibraryPath);
    
    public string OriginalsPath = "Resources/Audio/Originals";
    public string ControllersPath = "Resources/Audio/Controllers";
    public string SoundEventsPath = "Resources/Audio/SoundEvents";
    public string SoundBanksPath = "Resources/Audio/SoundBanks";
    public string MusicEventsPath = "Resources/Audio/MusicEvents";
    public string VoiceEventsPath = "Resources/Audio/VoiceEvents";
    public string StreamingClipsPath = "Resources/Audio/StreamingClips";
            
    public int MusicQuality = 50;
    public int SoundQuality = 40;
    public int VoiceQuality = 30;
}        
#endif