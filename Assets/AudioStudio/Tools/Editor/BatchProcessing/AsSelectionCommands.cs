using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AudioStudio.Tools
{
    public class AsSelectionCommands : EditorWindow
    {
				
        [MenuItem("Assets/AudioStudio/Batch Rename")]
        private static void BatchRename()
        {
            var window = GetWindow<AsAssetBatchRenamer>();			
            window.position = new Rect(800, 400, 200, 180);						
            window.titleContent = new GUIContent("Batch Rename");
        }
		
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
        
        [MenuItem("Assets/AudioStudio/Implementation Backup/Remove Selected")]
        public static void RemoveSelectedAssets()
        {
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(prefabs, AsComponentBackup.Instance.RemoveAllInPrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(scenes, AsComponentBackup.Instance.RemoveAllInScene);
            
            var clips = selectedPaths.Where(path => path.EndsWith(".anim")).ToArray();
            if (clips.Length > 0)
                AsAnimationEventBackup.Instance.RemoveSelectedAssets(clips, AsAnimationEventBackup.Instance.RemoveClip);

            var models = selectedPaths.Where(path => path.EndsWith(".FBX")).ToArray();
            if (models.Length > 0)
                AsAnimationEventBackup.Instance.RemoveSelectedAssets(models, AsAnimationEventBackup.Instance.RemoveModel);
            
            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.RemoveSelectedAssets(controllers, AsAudioStateBackup.Instance.RemoveAllInAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.RemoveSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.RemoveAllInTimeline);
        }
        
        [MenuItem("Assets/AudioStudio/Implementation Backup/Remove Unsaved")]
        public static void RemoveUnsavedComponents()
        {
            var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).ToArray();
            var prefabs = selectedPaths.Where(path => path.EndsWith(".prefab")).ToArray();
            if (prefabs.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(prefabs, AsComponentBackup.Instance.RemoveUnsavedInPrefab);
            
            var scenes = selectedPaths.Where(path => path.EndsWith(".unity")).ToArray();
            if (scenes.Length > 0)
                AsComponentBackup.Instance.RemoveSelectedAssets(scenes, AsComponentBackup.Instance.RemoveUnsavedInScene);

            var controllers = selectedPaths.Where(path => path.EndsWith(".controller")).ToArray();
            if (controllers.Length > 0)
                AsAudioStateBackup.Instance.RemoveSelectedAssets(controllers, AsAudioStateBackup.Instance.RemoveUnsavedInAnimator);
            
            var timelineAssets = selectedPaths.Where(path => path.EndsWith(".playable")).ToArray();
            if (timelineAssets.Length > 0)
                AsTimelineAudioBackup.Instance.RemoveSelectedAssets(timelineAssets, AsTimelineAudioBackup.Instance.RemoveUnsavedInTimeline);
        }

        [MenuItem("Assets/AudioStudio/Preview Timeline")]
        public static void PreviewTimeline()
        {
            AsTimelinePlayer.PreviewTimeline(Selection.activeGameObject);
        }
		
        [MenuItem("Assets/AudioStudio/Generate Bank Per Folder")]
        public static void GenerateBankPerFolder()
        {
            try
            {
                for (var i = 0; i < Selection.objects.Length; i++)
                {
                    var folderPath = AssetDatabase.GetAssetPath(Selection.objects[i]).Substring(7);
                    if (EditorUtility.DisplayCancelableProgressBar("Generating Banks", folderPath, (i + 1f) / Selection.objects.Length)) break;
                    var bankName = Selection.objects[i].name;
                    var bankPath = AsScriptingHelper.CombinePath(AudioPathSettings.Instance.SoundBanksPath, bankName + ".asset");
                    if (File.Exists(AsScriptingHelper.CombinePath(Application.dataPath, bankPath))) continue;
                    var newBank = CreateInstance<SoundBank>();
                    var contents = Directory.GetFiles(AsScriptingHelper.CombinePath(Application.dataPath, folderPath), "*.asset", SearchOption.AllDirectories);
                    foreach (var content in contents)
                    {
                        var sc = AssetDatabase.LoadAssetAtPath<SoundContainer>(AsScriptingHelper.ShortPath(content));
                        if (!sc) continue;
                        if (sc.IndependentEvent)
                            newBank.AudioEvents.Add(sc);
                    }
                    AssetDatabase.CreateAsset(newBank, "Assets/" + bankPath);
                }
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/AudioStudio/Filter Selection/Sound Containers")]
        private static void SelectSoundContainers()
        {
            var newSelection = new List<Object>(Selection.objects);
            foreach (var obj in Selection.objects)
            {
                if (!(obj is SoundContainer) || obj is SoundClip) newSelection.Remove(obj);
            }
            Selection.objects = newSelection.ToArray();
        }				
		
        [MenuItem("Assets/AudioStudio/Filter Selection/Sound Clips")]
        private static void SelectSoundClips()
        {
            Selection.objects = Selection.objects.OfType<SoundClip>().Cast<Object>().ToArray();
        }
		
        [MenuItem("Assets/AudioStudio/Filter Selection/Music Containers")]
        private static void SelectMusicContainers()
        {
            var newSelection = new List<Object>(Selection.objects);
            foreach (var obj in Selection.objects)
            {
                if (!(obj is MusicContainer) || obj is MusicTrack) newSelection.Remove(obj);
            }
            Selection.objects = newSelection.ToArray();
        }
		
        [MenuItem("Assets/AudioStudio/Filter Selection/Music Tracks")]
        private static void SelectMusicTracks()
        {
            Selection.objects = Selection.objects.OfType<MusicTrack>().Cast<Object>().ToArray();
        }
		
        [MenuItem("Assets/AudioStudio/Filter Selection/Voice Events")]
        private static void SelectVoiceEvents()
        {
            Selection.objects = Selection.objects.OfType<VoiceEvent>().Cast<Object>().ToArray();
        }
		
        [MenuItem("Assets/AudioStudio/Filter Selection/Independent Events")]
        private static void SelectIndependentEvents()
        {
            Selection.objects = Selection.objects.OfType<AudioEvent>().Where(e => e.IndependentEvent).Cast<Object>().ToArray();
        }
		
        [MenuItem("Assets/AudioStudio/Add To SoundBank")]
        private static void AddToSoundBank()
        {			
            var soundList = Selection.objects.OfType<SoundContainer>().ToArray();										
            AddSoundBankWindow.ShowWindow(soundList);
        }

        [MenuItem("Assets/AudioStudio/Select or Generate Events")]
        private static void GenerateEvents()
        {
            var clipList = Selection.objects.OfType<AudioClip>().ToArray();
            foreach (var audioClip in clipList)
            {
                var path = AssetDatabase.GetAssetPath(audioClip);
                if (path.Contains("Music"))
                {
                    var savePath = path.Replace(AudioPathSettings.Instance.OriginalsPath + "/Music", AudioPathSettings.Instance.MusicEventsPath)
                        .Replace(".wav", ".asset").Replace("Music_", "");

                    var savePathLong = Application.dataPath + savePath.Substring(6);
                    if (!File.Exists(savePathLong))
                    {
                        var track = CreateInstance<MusicTrack>();
                        track.name = audioClip.name.Substring(6);
                        track.Clip = audioClip;
                        AssetDatabase.CreateAsset(track, savePath);
                        Selection.activeObject = track;
                    }
                    else
                    {
                        var track = AssetDatabase.LoadAssetAtPath<MusicTrack>(savePath);
                        Selection.activeObject = track;
                    }					
                }
                else if (path.Contains("Voice"))
                {
                    var savePath = path.Replace(AudioPathSettings.Instance.OriginalsPath + "/Voice", AudioPathSettings.Instance.VoiceEventsPath)
                        .Replace(".wav", ".asset").Replace("Vo_", "");

                    var savePathLong = Application.dataPath + savePath.Substring(6);
                    if (!File.Exists(savePathLong))
                    {
                        var voiceEvent = CreateInstance<VoiceEvent>();
                        voiceEvent.name = audioClip.name.Substring(6);
                        voiceEvent.Clip = audioClip;
                        AssetDatabase.CreateAsset(voiceEvent, savePath);
                        Selection.activeObject = voiceEvent;
                    }
                    else
                    {
                        var voiceEvent = AssetDatabase.LoadAssetAtPath<VoiceEvent>(savePath);
                        Selection.activeObject = voiceEvent;
                    }					
                }
                else
                {
                    var savePath = path.Replace(AudioPathSettings.Instance.OriginalsPath + "/Sound", AudioPathSettings.Instance.SoundEventsPath)
                        .Replace(".wav", ".asset");
                    var savePathLong = Application.dataPath + savePath.Substring(6);
                    if (!File.Exists(savePathLong))
                    {
                        var sc = CreateInstance<SoundClip>();
                        sc.name = audioClip.name.Substring(6);
                        sc.Clip = audioClip;
                        AssetDatabase.CreateAsset(sc, savePath);
                        Selection.activeObject = sc;
                    }
                    else
                    {
                        var sc = AssetDatabase.LoadAssetAtPath<SoundClip>(savePath);
                        Selection.activeObject = sc;
                    }
                }
            }
        }
    }
	
    public class AddSoundBankWindow : EditorWindow
    {		
        public static void ShowWindow(SoundContainer[] soundList)
        {
            var window = GetWindow<AddSoundBankWindow>();			
            window.position = new Rect(800, 400, 200, 10);						
            window.titleContent = new GUIContent("Add To SoundBank");
            window._soundContainers = soundList;
        }

        private SoundBank _soundBank;		
        private SoundContainer[] _soundContainers;
		
        private void OnGUI()
        {
            _soundBank = EditorGUILayout.ObjectField(_soundBank, typeof(SoundBank),false) as SoundBank;
            if (GUILayout.Button("Add"))
            {
                if (_soundBank != null)
                {
                    foreach (var sc in _soundContainers)
                    {
                        _soundBank.RegisterEvent(sc);
                    }
                    EditorUtility.SetDirty(_soundBank);
                    AssetDatabase.SaveAssets();
                }								
                Close();
            }			
        }
    }
}