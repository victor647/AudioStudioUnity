using System.Diagnostics;
using UnityEditor;
using System.IO;
using AudioStudio.Midi;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;

namespace AudioStudio.Tools
{
    public partial class AsToolbarMenus : EditorWindow
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
                Process.Start(Path.Combine(Application.dataPath, AudioPathSettings.Instance.OriginalsPath));
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
            window.position = new Rect(500, 300, 500, 450);
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
        
        [MenuItem("AudioStudio/Tools/DLL Migration")]
        public static void DllMigration()
        {
            var sourcePath = EditorUtility.OpenFolderPanel("Root Folder","", "");
            try
            {
                CopyDllFile(sourcePath, "AudioStudio", true, false);
                CopyDllFile(sourcePath, "AudioStudio_Editor", true, false);
                CopyDllFile(sourcePath, "AudioStudio_Deployment", false, true);
                CopyDllFile(sourcePath, "AudioStudio_Deployment", true, true);
            }
#pragma warning disable 168
            catch(FileNotFoundException e)
#pragma warning restore 168
            {
                EditorUtility.DisplayDialog("Error", "Dll file not found!", "OK");
            }
        }
        
        private static void CopyDllFile(string sourcePath, string dllName, bool debugBuild, bool inSubFolder)
        {
            var subFolder = debugBuild ? "Debug" : "Release";
            var dllSource = AsScriptingHelper.CombinePath(sourcePath, dllName, "bin/" + subFolder, dllName + ".dll");
            var dllTarget = inSubFolder 
                ? AsScriptingHelper.CombinePath(AudioPathSettings.AudioStudioPluginPathFull, subFolder, dllName + ".dll") 
                : AsScriptingHelper.CombinePath(AudioPathSettings.AudioStudioPluginPathFull, dllName + ".dll");
            AsScriptingHelper.CheckoutLockedFile(dllTarget);
            File.Copy(dllSource, dllTarget, true);
            if (debugBuild)
            {
                var pdbSource = dllSource.Replace(".dll", ".pdb");
                var pdbTarget = dllTarget.Replace(".dll", ".pdb");
                AsScriptingHelper.CheckoutLockedFile(pdbTarget);
                File.Copy(pdbSource, pdbTarget, true);
            }
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
        
        [MenuItem("AudioStudio/Tools/Script Reference Update")]
        public static void ScriptReferenceUpdate()
        {
            var window = GetWindow<AsScriptReferenceUpdate>();
            window.position = new Rect(500, 300, 400, 100);
            window.titleContent = new GUIContent("Script Reference Update");
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
        
        [MenuItem("AudioStudio/Tools/Character Component Sync")]
        public static void CharacterComponentSync()
        {
            var window = GetWindow<CharacterComponentSync>();
            window.position = new Rect(500, 300, 300, 100);
            window.titleContent = new GUIContent("Character Component Sync");
        }

        [MenuItem("AudioStudio/Tools/Build AssetBundles")]
        private static void BuildAssetBundles()
        {
            AudioAssetBundleBuilder.BuildAssetBundles();
        }
        #endregion

        #region VersionControl
#if UNITY_EDITOR_WIN
        [MenuItem("AudioStudio/SVN/Update Game Project")]
        public static void SvnUpdateProject()
        {
            var arguments = string.Format("/command:update /path:\"{0}\"", Application.dataPath);
            AsScriptingHelper.RunCommand("TortoiseProc.exe", arguments);
        }
        
        [MenuItem("AudioStudio/SVN/Revert Game Project")]
        public static void SvnRevertProject()
        {            
            var arguments = string.Format("/command:revert /path:\"{0}\"", Directory.GetParent(Application.dataPath));
            AsScriptingHelper.RunCommand("TortoiseProc.exe", arguments);                      
        }
        
        [MenuItem("AudioStudio/SVN/Submit Originals and Assets &F8")]
        public static void SvnSubmitAssets()
        {
            var path = AsScriptingHelper.CombinePath(Application.dataPath, AudioPathSettings.Instance.AudioResourcesPath);
            AsScriptingHelper.RunCommand("TortoiseProc.exe", $"/command:add /path:\"{path}\"");
            AsScriptingHelper.RunCommand("TortoiseProc.exe", $"/command:commit /path:\"{path}\" /logmsg \"{SubmitAudioResourcesDescription}\"");
        }

        [MenuItem("AudioStudio/SVN/Submit AudioStudio &F12")]
        public static void SvnSubmitAudioStudio()
        {
            var path = AudioPathSettings.AudioStudioLibraryPathFull;
            AsScriptingHelper.RunCommand("TortoiseProc.exe", $"/command:add /path:\"{path}\"");
            AsScriptingHelper.RunCommand("TortoiseProc.exe", $"/command:commit /path:\"{path}\" /logmsg \"{SubmitAudioStudioDescription}\"");
            path = AudioPathSettings.AudioStudioPluginPathFull;
            AsScriptingHelper.RunCommand("TortoiseProc.exe", $"/command:commit /path:\"{path}\" /logmsg \"{SubmitAudioStudioDescription}\"");
        }
#endif
        
        [MenuItem("AudioStudio/Perforce/Update Game Project")]
        public static void P4UpdateProject()
        {                
            GetWorkSpaceName();
            Process.Start("p4", string.Format("-p {0} sync {1}/...#head", PerforcePort, Directory.GetParent(Application.dataPath)));
        }
                
        [MenuItem("AudioStudio/Perforce/Submit AudioStudio Library")]
        public static void P4SubmitAudioStudio()
        {
            GetWorkSpaceName();
            var path = AudioPathSettings.AudioStudioLibraryPathFull;
            var command = string.Format("-p {0} submit -f revertunchanged -d \"{1}\" {2}/...", PerforcePort, SubmitAudioStudioDescription, path);
            Process.Start("p4", command);            
        }

        [MenuItem("AudioStudio/Perforce/Checkout Audio Resources")]
        public static void P4CheckoutResources()
        {                
            GetWorkSpaceName();
            var path = AudioPathSettings.Instance.AudioResourcesPath;   
            Process.Start("p4", string.Format("-p {0} edit {1}/...", PerforcePort, path));
        }
                
        [MenuItem("AudioStudio/Perforce/Submit Audio Resources")]
        public static void P4SubmitResources()
        {           
            GetWorkSpaceName();
            var path = AudioPathSettings.Instance.AudioResourcesPath;          
            Process.Start("p4", string.Format("-p {0} submit -f revertunchanged -d \"{1}\" {2}/...", PerforcePort, SubmitAudioResourcesDescription, path));
        }
                
        private static void GetWorkSpaceName()
        {
            var task = Provider.UpdateSettings();
            task.Wait();
            var workSpaceName = task.messages[87].message.Split('"')[1];
            Process.Start("p4", string.Format("set p4client={0}", workSpaceName));
        }
        #endregion
    }
}