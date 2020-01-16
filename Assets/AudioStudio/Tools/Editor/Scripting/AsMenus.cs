using System.Diagnostics;
using UnityEditor;
using System.IO;
using System.Linq;
using AudioStudio.Midi;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class AsMenus : EditorWindow
    {
        #region Configs
        [MenuItem("AudioStudio/Configs/Audio Init &F1")]
        private static void AudioInitSettings()
        {
            Selection.activeObject = AudioStudio.AudioInitSettings.Instance;
        }

        [MenuItem("AudioStudio/Configs/Audio Path Settings &F2")]
        private static void AudioSettings()
        {
            Selection.activeObject = AudioPathSettings.Instance;
        }
        #endregion

        #region Open
        [MenuItem("AudioStudio/Open/Start Game")]
        public static void StartGame()
        {
            EditorSceneManager.OpenScene("Assets/" + AudioPathSettings.Instance.StartScenePath);
            EditorApplication.isPlaying = true;
        }
        
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
                Process.Start(Path.Combine(Application.dataPath, AudioPathSettings.Instance.SoundFilesPath));
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
            var window = GetWindow<AsBatchProcessor>();
            window.position = new Rect(500, 300, 400, 300);
            window.titleContent = new GUIContent("Audio Batch Processor");
        }

        [MenuItem("AudioStudio/Tools/Implementation Backup  &F9")]
        public static void AudioStudioBackUp()
        {
            var window = GetWindow<AsBackupWindow>();
            window.position = new Rect(500, 300, 500, 430);
            window.titleContent = new GUIContent("BackUp");
        }

        [MenuItem("AudioStudio/Tools/Animation Player &F10")]
        private static void AnimationPlayer()
        {
            var window = GetWindow<AsAnimationPlayer>();
            window.position = new Rect(500, 300, 320, 400);
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

        [MenuItem("AudioStudio/Tools/Build AssetBundles")]
        private static void BuildAssetBundles()
        {
            AudioAssetBundleBuilder.BuildAssetBundles();
        }
        #endregion
        
        #region RightClick

        [MenuItem("Assets/AudioStudio/Implementation Backup/Save Selection")]
        public static void SaveSelectedAssets()
        {
            AsComponentBackup.Instance.SeparateXmlFiles = false;
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.SaveSelectedAssets(prefabs, AsComponentBackup.Instance.ParsePrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.SaveSelectedAssets(scenes, AsComponentBackup.Instance.ParseScene);
            
            var clips = selectedPaths.Where(path => path.EndsWith(".anim")).ToArray();
            if (clips.Length > 0)
                AsAnimationEventBackup.Instance.SaveSelectedAssets(clips, AsAnimationEventBackup.Instance.ParseAnimation);
            
            var models = selectedPaths.Where(path => path.EndsWith(".FBX")).ToArray();
            if (models.Length > 0)
                AsAnimationEventBackup.Instance.SaveSelectedAssets(models, AsAnimationEventBackup.Instance.ParseModel);
            
            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.SaveSelectedAssets(controllers, AsAudioStateBackup.Instance.ParseAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.SaveSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.ParseTimeline);
        }

        [MenuItem("Assets/AudioStudio/Implementation Backup/Export Selection")]
        public static void ExportSelectedAssets()
        {
            AsComponentBackup.Instance.SeparateXmlFiles = false;
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.ExportSelectedAssets(prefabs, AsComponentBackup.Instance.ParsePrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.ExportSelectedAssets(scenes, AsComponentBackup.Instance.ParseScene);
            
            var clips = selectedPaths.Where(path => path.EndsWith(".anim")).ToArray();
            if (clips.Length > 0)
                AsAnimationEventBackup.Instance.ExportSelectedAssets(clips, AsAnimationEventBackup.Instance.ParseAnimation);
            
            var models = selectedPaths.Where(path => path.EndsWith(".FBX")).ToArray();
            if (models.Length > 0)
                AsAnimationEventBackup.Instance.ExportSelectedAssets(models, AsAnimationEventBackup.Instance.ParseModel);
            
            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.ExportSelectedAssets(controllers, AsAudioStateBackup.Instance.ParseAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.ExportSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.ParseTimeline);
        }
        
        [MenuItem("Assets/AudioStudio/Implementation Backup/Revert Selection")]
        public static void RevertSelectedAssets()
        {
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.RevertSelectedAssets(prefabs, AsComponentBackup.Instance.ImportPrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.RevertSelectedAssets(scenes, AsComponentBackup.Instance.ImportScene);
            
            var clips = selectedPaths.Where(path => path.EndsWith(".anim")).ToArray();
            if (clips.Length > 0)
                AsAnimationEventBackup.Instance.RevertSelectedAssets(clips, AsAnimationEventBackup.Instance.ImportClip);
            
            var models = selectedPaths.Where(path => path.EndsWith(".FBX")).ToArray();
            if (models.Length > 0)
                AsAnimationEventBackup.Instance.RevertSelectedAssets(models, AsAnimationEventBackup.Instance.ImportModel);
            
            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.RevertSelectedAssets(controllers, AsAudioStateBackup.Instance.ImportAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.RevertSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.ImportTimeline);
        }
        
        [MenuItem("Assets/AudioStudio/Implementation Backup/Remove Selection")]
        public static void RemoveSelectedAssets()
        {
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(prefabs, AsComponentBackup.Instance.RemovePrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(scenes, AsComponentBackup.Instance.RemoveScene);
            
            var clips = selectedPaths.Where(path => path.EndsWith(".anim")).ToArray();
            if (clips.Length > 0)
                AsAnimationEventBackup.Instance.RemoveSelectedAssets(clips, AsAnimationEventBackup.Instance.RemoveClip);
            
            var models = selectedPaths.Where(path => path.EndsWith(".FBX")).ToArray();
            if (models.Length > 0)
                AsAnimationEventBackup.Instance.RemoveSelectedAssets(models, AsAnimationEventBackup.Instance.RemoveModel);
            
            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.RemoveSelectedAssets(controllers, AsAudioStateBackup.Instance.RemoveAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.RemoveSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.RemoveTimeline);
        }

        [MenuItem("Assets/AudioStudio/Preview Timeline")]
        public static void PreviewTimeline()
        {
            AsTimelinePlayer.PreviewTimeline(Selection.activeGameObject);
        }
        #endregion
        
        #region VersionControl
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
        
        [MenuItem("AudioStudio/SVN/Submit AudioStudio Plugins")]
        public static void AddPluginsToSvn()
        {
            AddSubmitToSvn("Assets/" + AudioPathSettings.AudioStudioPluginPath);
        }

        private static void AddSubmitToSvn(string folderPath)
        {
            var arguments = $"/command:add /path:\"{folderPath}\"";
            AsScriptingHelper.RunCommand("TortoiseProc.exe", arguments);
            arguments = $"/command:commit /path:\"{folderPath}\"";
            AsScriptingHelper.RunCommand("TortoiseProc.exe", arguments);
        }

        [MenuItem("AudioStudio/SVN/Update Game Project")]
        public static void SvnUpdateProject()
        {
            var arguments = string.Format("/command:update /path:\"{0}\"", Application.dataPath);
            AsScriptingHelper.RunCommand("TortoiseProc.exe", arguments);
        }
        #endregion
    }
}