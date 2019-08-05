using System.Diagnostics;
using UnityEditor;
using System.IO;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class AsMenus : EditorWindow
    {
        #region Configs
        [MenuItem("AudioStudio/Configs/Audio Init &F2")]
        private static void AudioInitSettings()
        {
            Selection.activeObject = AudioStudio.AudioInitSettings.Instance;
        }

        [MenuItem("AudioStudio/Configs/Audio Editor Settings &F4")]
        private static void AudioSettings()
        {
            Process.Start(Path.Combine(AudioPathSettings.AudioStudioLibraryPathFull, "Extensions", "AudioPathSettings.cs"));
        }
        #endregion

        #region Open
        [MenuItem("AudioStudio/Open/Audio Profiler &F6")]
        public static void AudioProfiler()
        {
            GetWindow<AudioProfiler>("Audio Profiler", true,
                typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow"));
        }
        
        [MenuItem("AudioStudio/Open/MIDI Console")]
        public static void MidiConsole()
        {
            GetWindow<MidiConsole>("Midi Console", true,
                typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow"));
        }

        [MenuItem("AudioStudio/Open/SFX Folder &F8")]
        private static void OpenSfxFolder()
        {
            try
            {
                Process.Start(Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath));
            }
            catch
            {
                EditorUtility.DisplayDialog("Error", "Can't find SFX folder!", "OK");
            }
        }
        #endregion

        #region Tools    
        [MenuItem("AudioStudio/Tools/Audio Batch Processor &F5")]
        public static void AudioBatchProcessor()
        {
            var window = (AudioBatchProcessor)GetWindow(typeof(AudioBatchProcessor));
            window.position = new Rect(500, 300, 400, 260);
            window.titleContent = new GUIContent("Audio Batch Processor");
        }
        
        [MenuItem("Assets/音频/恢复Prefab音效配置")]
        public static void RevertComponentsToPrefab()
        {
            AsComponentBackup.Instance.RevertPrefab(Selection.activeObject.name);
        }        
        
        [MenuItem("Assets/音频/恢复动作音效配置")]
        public static void RevertAnimationEvents()
        {
            AsAnimationEventBackup.Instance.RevertClip(Selection.activeObject);
        }  
        
        [MenuItem("AudioStudio/Tools/Implementation Backup  &F9")]
        public static void AudioStudioBackUp()
        {
            var window = GetWindow<AsImplementationBackup>();
            window.position = new Rect(500, 300, 500, 400);
            window.titleContent = new GUIContent("BackUp");
        }

        [MenuItem("AudioStudio/Tools/Animation Player &F10")]
        private static void AnimationPlayer()
        {
            var window = GetWindow<AsAnimationPlayer>();
            window.position = new Rect(500, 300, 320, 300);
            window.titleContent = new GUIContent("Animation Player");
        }
        
        [MenuItem("AudioStudio/Tools/Script Migration &F11")]
        public static void ScriptMigration()
        {
            var window = GetWindow<AsScriptMigration>();
            window.position = new Rect(500, 300, 690, 360);
            window.titleContent = new GUIContent("Script Migration");
        }

        [MenuItem("AudioStudio/Tools/Replace by Regex in Text File")]
        public static void RegexReplacer()
        {
            var window = GetWindow<RegexReplacer>();
            window.position = new Rect(500, 300, 300, 150);
            window.titleContent = new GUIContent("Regex Replacer");
        }

        [MenuItem("AudioStudio/Tools/Field Upgrade")]
        public static void FieldUpgrade()
        {
            var window = GetWindow<AsFieldUpgrade>();
            window.position = new Rect(500, 300, 300, 150);
            window.titleContent = new GUIContent("Filed Upgrade");
        }

        [MenuItem("AudioStudio/Tools/Remove Missing and Duplicate Components")]
        public static void RemoveMissingDuplicate()
        {
            var window = GetWindow<RemoveMissingDuplicate>();
            window.position = new Rect(500, 300, 500, 120);
            window.titleContent = new GUIContent("Remove");
        }

        [MenuItem("AudioStudio/Tools/Search Linked Components")]
        private static void SearchLinkedComponents()
        {
            var window = GetWindow<AsComponentLink>();
            window.position = new Rect(500, 300, 500, 500);
        }
        #endregion
        
        #region VersionControl
#if UNITY_EDITOR_WIN
        [MenuItem("AudioStudio/SVN/Submit Originals and Assets &F8")]
        public static void AddSoundsToSvn()
        {
            AddSubmitToSvn("Assets/Art/Audio");
            AddSubmitToSvn("Assets/ResourcesAssets/Audio");
        }

        [MenuItem("AudioStudio/SVN/Submit AudioStudio Library")]
        public static void AddLibraryToSvn()
        {
            AddSubmitToSvn("Assets/" + AudioPathSettings.AudioStudioLibraryPath);
        }

        private static void AddSubmitToSvn(string folderPath)
        {
            var arguments = $"/command:add /path:\"{folderPath}\"";
            AudioUtility.RunCommand("TortoiseProc.exe", arguments);
            arguments = $"/command:commit /path:\"{folderPath}\"";
            AudioUtility.RunCommand("TortoiseProc.exe", arguments);
        }

        [MenuItem("AudioStudio/SVN/Update Game Project")]
        public static void SvnUpdateProject()
        {
            var arguments = string.Format("/command:update /path:\"{0}\"", Application.dataPath);
            AudioUtility.RunCommand("TortoiseProc.exe", arguments);
        }
#endif
        #endregion
    }
}