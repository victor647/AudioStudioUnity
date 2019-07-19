using System.Diagnostics;
using UnityEditor;
using System.IO;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
    public class AsMenus : EditorWindow
    {
        #region Open

        #region Open		

        [MenuItem("AudioStudio/Open SFX Folder &F1")]
        private static void OpenSFXFolder()
        {
            try
            {
                Process.Start(Path.Combine(Application.dataPath, "Art/Audio/Originals"));
            }
            catch
            {
                EditorUtility.DisplayDialog("Error", "Can't find SFX folder!", "OK");
            }
        }

        #endregion

        #endregion

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

        #region Window

        [MenuItem("AudioStudio/Audio Profiler &F6")]
        public static void AudioProfiler()
        {
            var window = (AudioProfiler)GetWindow(typeof(AudioProfiler));
            window.position = new Rect(500, 300, 700, 500);
            window.titleContent = new GUIContent("Audio Profiler");
        }       
        #endregion

        #region Searchers    

        [MenuItem("AudioStudio/Import and Export/AudioStudio Components &F9")]
        public static void ComponentImportExport()
        {
            var window = (AsComponentBackup) GetWindow(typeof(AsComponentBackup));
            window.position = new Rect(500, 300, 500, 400);
            window.titleContent = new GUIContent("Import/Export");
        }

        [MenuItem("AudioStudio/Import and Export/Animation Events &F10")]
        public static void AnimationEventSearcher()
        {
            var window = (AsAnimationEventBackup) GetWindow(typeof(AsAnimationEventBackup));
            window.position = new Rect(500, 300, 500, 100);
            window.titleContent = new GUIContent("Animation Events");
        }

        [MenuItem("AudioStudio/Import and Export/Timeline Assets &F11")]
        public static void TimelineSearcher()
        {
            var window = (AsTimelineAudioBackup) GetWindow(typeof(AsTimelineAudioBackup));
            window.position = new Rect(500, 300, 500, 100);
            window.titleContent = new GUIContent("Timeline Assets");
        }

        [MenuItem("AudioStudio/Import and Export/AudioStates &F12")]
        public static void AudioStateSearcher()
        {
            var window = (AsAudioStateBackup) GetWindow(typeof(AsAudioStateBackup));
            window.position = new Rect(500, 300, 500, 100);
            window.titleContent = new GUIContent("AudioStates");
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
        
        [MenuItem("AudioStudio/Tools/Script Migration &F8")]
        public static void ScriptMigration()
        {
            var window = (AsScriptMigration) GetWindow(typeof(AsScriptMigration));
            window.position = new Rect(500, 300, 690, 360);
            window.titleContent = new GUIContent("Script Migration");
        }

        [MenuItem("AudioStudio/Tools/Replace by Regex in Text File")]
        public static void RegexReplacer()
        {
            var window = (RegexReplacer) GetWindow(typeof(RegexReplacer));
            window.position = new Rect(500, 300, 300, 150);
            window.titleContent = new GUIContent("Regex Replacer");
        }

        [MenuItem("AudioStudio/Tools/Field Upgrade")]
        public static void FieldUpgrade()
        {
            var window = (AsFieldUpgrade) GetWindow(typeof(AsFieldUpgrade));
            window.position = new Rect(500, 300, 300, 150);
            window.titleContent = new GUIContent("Filed Upgrade");
        }

        [MenuItem("AudioStudio/Tools/Remove Missing and Duplicate Components")]
        public static void RemoveMissingDuplicate()
        {
            var window = (RemoveMissingDuplicate) GetWindow(typeof(RemoveMissingDuplicate));
            window.position = new Rect(500, 300, 500, 120);
            window.titleContent = new GUIContent("Remove");
        }

        [MenuItem("AudioStudio/Tools/Search Linked Components")]
        private static void ComponentSearchAddRemove()
        {
            AsComponentLink window = (AsComponentLink) GetWindow(typeof(AsComponentLink));
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