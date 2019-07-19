using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace AudioStudio
{        
    public class AudioBatchProcessor : EditorWindow
    {		
        private Platform _platform;        
        
        #region GUI
        private void OnGUI()
        {                    
            DrawConfiguration();
            DrawImportSettings();            
            GUI.backgroundColor = Color.green;
            GUI.contentColor = Color.yellow;
            if (GUILayout.Button("ONE CLICK REFRESH"))
            {
                SetStreaming();
                SetMusicQuality();                     
                GenerateSoundClips(false);
                GenerateSoundContainers(false);
                GenerateMusicTracks(false);
                GenerateMusicContainers(false);
                GenerateVoiceEvents(false);
                CleanUpEmptyFields(true);                                                                                           
            }
        }           
        
        private void DrawConfiguration()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Generate Events for Platform", EditorStyles.boldLabel);  
            _platform = (Platform) EditorGUILayout.EnumPopup(_platform);
            GUILayout.EndHorizontal();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sound Clips")) GenerateSoundClips(true);
                if (GUILayout.Button("Sound Containers")) GenerateSoundContainers(true);                
                GUILayout.EndHorizontal();                                 
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Music Tracks")) GenerateMusicTracks(true);
                if (GUILayout.Button("Music Containers")) GenerateMusicContainers(true);
                GUILayout.EndHorizontal();                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Voice Events")) GenerateVoiceEvents(true); 
                if (GUILayout.Button("Clean Up Events & Banks")) CleanUpEmptyFields(true); 
                GUILayout.EndHorizontal();                
                if (GUILayout.Button("Set Default Spatial Setting to 3D Sounds")) SetSpatialSettings();
            }            
        }       
        
        private void DrawImportSettings()
        {
            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel); 
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {               
                DrawQuality("Music", ref AudioImportProcessor.MusicQuality, SetMusicQuality);    
                DrawQuality("Sound", ref AudioImportProcessor.SoundQuality, SetSoundQuality);
                DrawQuality("Voice", ref AudioImportProcessor.VoiceQuality, SetVoiceQuality);
                                                             
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set SFX to ");
                if (GUILayout.Button("Compressed in Memory")) SetCompressedInMemory();     
                if (GUILayout.Button("Decompress on Load")) SetDecompressOnLoad();
                GUILayout.EndHorizontal();                
                if (GUILayout.Button("Set Music/Voice/Ambience to Streaming")) SetStreaming();
            }
        }

        private void DrawQuality(string label, ref int quality, Action setQuality)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " Quality");
            quality = EditorGUILayout.IntSlider(quality, 10, 100);
            if (GUILayout.Button("Apply", EditorStyles.miniButtonRight, GUILayout.Width(50))) setQuality();
            GUILayout.EndHorizontal(); 
        }
        #endregion
		
        #region GenerateAssets
        private void GenerateMusicTracks(bool saveAssets)
        {
            string sourceFolder;
            string extension;
            if (_platform == Platform.Web)
            {
                sourceFolder = Path.Combine(AudioPathSettings.StreamingClipsPath, "Music");
                extension = ".ogg";
            }
            else
            {
                sourceFolder = Path.Combine(AudioPathSettings.OriginalsPath, "Music");
                extension = ".wav";
            }
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio files...", 0);				
            string[] audioFilePaths = Directory.GetFiles(Path.Combine(Application.dataPath, sourceFolder), "*" + extension, SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {
                var savePathLong = audioFilePaths[i].Replace(sourceFolder, AudioPathSettings.MusicEventsPath).Replace(extension, ".asset").Replace("Music_", "");                
                var loadPathShort = audioFilePaths[i].Substring(audioFilePaths[i].IndexOf("Assets", StringComparison.Ordinal));
                var savePathShort = savePathLong.Substring(savePathLong.IndexOf("Assets", StringComparison.Ordinal));                
                if (EditorUtility.DisplayCancelableProgressBar("Generating Music Tracks", loadPathShort, (i + 1) * 1.0f / audioFilePaths.Length)) break;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(loadPathShort);
                
                //check if asset already exists but missing audio file
                if (File.Exists(savePathLong))
                {
                    var mc = AssetDatabase.LoadAssetAtPath<MusicTrack>(savePathShort);
                    if (mc && !mc.Clip)
                    {
                        mc.Clip = clip;
                        EditorUtility.SetDirty(mc);
                    }    
                    continue;
                }               
                savePathLong = Path.GetDirectoryName(savePathLong);								
                if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);												                
                var newTrack = CreateInstance<MusicTrack>();
                newTrack.name = clip.name.Substring(6);                
                if (_platform == Platform.Web)
                    newTrack.Platform = Platform.Web;
                else
                    newTrack.Clip = clip;    
                AssetDatabase.CreateAsset(newTrack, savePathShort);
            }		
            if (saveAssets) AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();   
        }
        
        private void GenerateSoundClips(bool saveAssets)
        {
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio files...", 0);
            var searchPath = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Sound");
            string[] audioFilePaths = Directory.GetFiles(searchPath, "*.wav", SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {				
                var savePathLong = audioFilePaths[i].Replace(Path.Combine(AudioPathSettings.OriginalsPath, "Sound"), AudioPathSettings.SoundEventsPath).Replace(".wav", ".asset");				
                var loadPathShort = audioFilePaths[i].Substring(audioFilePaths[i].IndexOf("Assets", StringComparison.Ordinal));
                var savePathShort = savePathLong.Substring(savePathLong.IndexOf("Assets", StringComparison.Ordinal));
                if (EditorUtility.DisplayCancelableProgressBar("Generating Sound Clips", loadPathShort, (i + 1) * 1.0f / audioFilePaths.Length)) break;
				
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(loadPathShort);                
                
                //check if asset already exists but missing audio file
                if (File.Exists(savePathLong))
                {
                    var sc = AssetDatabase.LoadAssetAtPath<SoundClip>(savePathShort);
                    if (sc && !sc.Clip)
                    {
                        sc.Clip = clip;
                        EditorUtility.SetDirty(sc);
                    }    
                    continue;
                }               
                
                savePathLong = Path.GetDirectoryName(savePathLong);								
                if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);				
                var newClip = CreateInstance<SoundClip>();	                
                newClip.name = clip.name;				
                newClip.Clip = clip;                
                AssetDatabase.CreateAsset(newClip, savePathShort);	
            }		
            if (saveAssets) AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();    
        }

        private void GenerateSoundContainers(bool saveAssets)
        {
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching sound clips...", 0);
            var searchPath = Path.Combine(Application.dataPath, AudioPathSettings.SoundEventsPath);
            string[] soundClipPaths = Directory.GetFiles(searchPath, "*.asset", SearchOption.AllDirectories);
            for (var i = 0; i < soundClipPaths.Length; i++)
            {				                			
                var loadPathShort = soundClipPaths[i].Substring(soundClipPaths[i].IndexOf("Assets", StringComparison.Ordinal));
                if (EditorUtility.DisplayCancelableProgressBar("Generating Sound Containers", loadPathShort, (i + 1) * 1.0f / soundClipPaths.Length)) break;
				
                var clip = AssetDatabase.LoadAssetAtPath<SoundClip>(loadPathShort);

                var result = Regex.Match(loadPathShort, @"_\d*.asset$");
                if (result.Success)
                {
                    var eventNameShort = loadPathShort.Remove(result.Index);
                    var eventNameLong = soundClipPaths[i].Replace(result.Value, "");
                    if (File.Exists(eventNameLong + ".asset"))
                    {						                        
                        var randomContainer =  AssetDatabase.LoadAssetAtPath<SoundContainer>(eventNameShort + ".asset");
                        if (!randomContainer) continue;
                        if (randomContainer.ChildEvents.Contains(clip)) continue;
                        EditorUtility.SetDirty(randomContainer);
                        randomContainer.ChildEvents.Add(clip);                                                
                        clip.IndependentEvent = false;
                    }
                    else
                    {
                        var savePathLong = Path.GetDirectoryName(soundClipPaths[i]);	
                        if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);						
                        var eventName = Path.GetFileName(eventNameLong);                                                    
                        var newRandomEvent = CreateInstance<SoundContainer>();                        
                        newRandomEvent.PlayLogic = SoundPlayLogic.Random;
                        newRandomEvent.name = eventName;				
                        newRandomEvent.ChildEvents.Add(clip);
                        AssetDatabase.CreateAsset(newRandomEvent, eventNameShort + ".asset");
                    }                    
                }	                                
            }		
            if (saveAssets) AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();   
        }
        
        private void GenerateMusicContainers(bool saveAssets)
        {
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching music tracks...", 0);
            var searchPath = Path.Combine(Application.dataPath, AudioPathSettings.MusicEventsPath);
            string[] musicTrackPaths = Directory.GetFiles(searchPath, "*.asset", SearchOption.AllDirectories);
            for (var i = 0; i < musicTrackPaths.Length; i++)
            {                	
                var loadPathShort = musicTrackPaths[i].Substring(musicTrackPaths[i].IndexOf("Assets", StringComparison.Ordinal));                
                if (EditorUtility.DisplayCancelableProgressBar("Generating Music Containers", loadPathShort, (i + 1) * 1.0f / musicTrackPaths.Length)) break;
				
                var track = AssetDatabase.LoadAssetAtPath<MusicTrack>(loadPathShort);	                
                
                //for random
                var result = Regex.Match(loadPathShort, @"_\d*.asset$");
                if (result.Success) 
                {
                    var eventPathShort = loadPathShort.Remove(result.Index);
                    var eventPathLong = musicTrackPaths[i].Replace(result.Value, "");
                    if (File.Exists(eventPathLong + ".asset"))                    						                        
                        CheckExistingMusicContainer(eventPathShort, track);                    
                    else                    
                        AddToMusicContainer(eventPathLong, eventPathShort, track, MusicPlayLogic.Random);                     
                }	 
                
                //for sequence
                var indexIntro = loadPathShort.IndexOf("_Intro", StringComparison.Ordinal);
                if (indexIntro > 0)
                {
                    var eventPathShort = loadPathShort.Remove(indexIntro);
                    var eventPathLong = musicTrackPaths[i].Replace("_Intro", "");
                    if (!File.Exists(eventPathLong))                                     
                        AddToMusicContainer(eventPathLong, eventPathShort, track, MusicPlayLogic.SequenceContinuous);                     
                }
                var indexLoop = loadPathShort.IndexOf("_Loop", StringComparison.Ordinal);
                if (indexLoop > 0)
                {
                    var eventPathShort = loadPathShort.Remove(indexLoop);
                    var eventPathLong = musicTrackPaths[i].Replace("_Loop", "");
                    if (File.Exists(eventPathLong))                    
                        CheckExistingMusicContainer(eventPathShort, track);                                        
                }
            }
            if (saveAssets) AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();   
        }

        private void CheckExistingMusicContainer(string eventPath, MusicTrack track)
        {
            var mc = AssetDatabase.LoadAssetAtPath<MusicContainer>(eventPath + ".asset");
            if (!mc || mc.ChildEvents.Contains(track)) return;  
            mc.ChildEvents.Add(track);
            track.IndependentEvent = false;
            EditorUtility.SetDirty(mc);
        }

        private void AddToMusicContainer(string eventPathLong, string eventPathShort, MusicTrack track, MusicPlayLogic playLogic)
        {
            var savePathLong = Path.GetDirectoryName(eventPathLong);	
            if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);						
            var eventName = Path.GetFileName(eventPathLong);                                                    
            var newMusicContainer = CreateInstance<MusicContainer>();
            newMusicContainer.PlayLogic = playLogic;
            newMusicContainer.name = eventName;				
            newMusicContainer.ChildEvents.Add(track);                                                                    
            AssetDatabase.CreateAsset(newMusicContainer, eventPathShort + ".asset");
        }
        
        private void GenerateVoiceEvents(bool saveAssets)
        {
            string sourceFolder;
            string extension;
            if (_platform == Platform.Web)
            {
                sourceFolder = Path.Combine(AudioPathSettings.StreamingClipsPath, "Voice");
                extension = ".ogg";
            }
            else
            {
                sourceFolder = Path.Combine(AudioPathSettings.OriginalsPath, "Voice");
                extension = ".wav";
            }
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio files...", 0);				
            string[] audioFilePaths = Directory.GetFiles(Path.Combine(Application.dataPath, sourceFolder), "*" + extension, SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {
                var savePathLong = audioFilePaths[i].Replace(sourceFolder, AudioPathSettings.VoiceEventsPath).Replace(extension, ".asset").Replace("Vo_", "");				
                var loadPathShort = audioFilePaths[i].Substring(audioFilePaths[i].IndexOf("Assets", StringComparison.Ordinal));
                var savePathShort = savePathLong.Substring(savePathLong.IndexOf("Assets", StringComparison.Ordinal));
                if (EditorUtility.DisplayCancelableProgressBar("Generating Voice Events", loadPathShort, (i + 1) * 1.0f / audioFilePaths.Length)) break;
				
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(loadPathShort);	
                
                var result = Regex.Match(savePathLong, @"_\d*.asset$");
                if (result.Success)
                {
                    var eventNameShort = savePathShort.Replace(result.Value, "");
                    var eventNameLong = savePathLong.Remove(result.Index);			
                    if (File.Exists(eventNameLong + ".asset"))
                    {
                        var randomEvent = AssetDatabase.LoadAssetAtPath<VoiceEvent>(eventNameShort + ".asset");
                        if (randomEvent)
                        {
                            EditorUtility.SetDirty(randomEvent);
                            if (_platform != Platform.Web)
                            {                            
                                if (!randomEvent.Clips.Contains(clip)) randomEvent.Clips.Add(clip);                            
                            }
                            else
                            {
                                if (randomEvent.ClipCount < int.Parse(result.Value.Substring(1, 2))) randomEvent.ClipCount++;
                            }
                        }                        
                    }
                    else
                    {
                        savePathLong = Path.GetDirectoryName(savePathLong);	
                        if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);						                                                                          
                        var newRandomEvent = CreateInstance<VoiceEvent>();
                        newRandomEvent.PlayLogic = VoicePlayLogic.Random;
                        newRandomEvent.name = clip.name.Replace(result.Value, "");
                        if (_platform != Platform.Web)
                            newRandomEvent.Clips.Add(clip);
                        else
                        {
                            newRandomEvent.Platform = Platform.Web;
                            newRandomEvent.ClipCount++;
                        }
                        AssetDatabase.CreateAsset(newRandomEvent, eventNameShort + ".asset");
                    }
                    continue;
                }	
                
                //check if asset already exists but missing audio file
                if (File.Exists(savePathLong))
                {
                    var ve = AssetDatabase.LoadAssetAtPath<VoiceEvent>(savePathShort);
                    if (ve && !ve.Clip)
                    {
                        ve.Clip = clip;
                        EditorUtility.SetDirty(ve);
                    }    
                    continue;
                }
                
                savePathLong = Path.GetDirectoryName(savePathLong);								
                if (!Directory.Exists(savePathLong)) Directory.CreateDirectory(savePathLong);				
                var newEvent = CreateInstance<VoiceEvent>();	
                newEvent.name = clip.name;
                if (_platform == Platform.Web)
                    newEvent.Platform = Platform.Web;
                else
                    newEvent.Clip = clip;                    
                AssetDatabase.CreateAsset(newEvent, savePathShort);	
            }		
            if (saveAssets) AssetDatabase.SaveAssets();	
            EditorUtility.ClearProgressBar();   
        }        
        #endregion
        
        #region Reimport        
        private Action<string, float> ReimportClips;        
		
        private void SetMusicQuality()
        {
            ReimportClips = SetQuality;
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Music");
            var musicFiles = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);			
            IterateAudioFiles(musicFiles, "Setting Music Quality", AudioImportProcessor.MusicQuality);
            EditorUtility.ClearProgressBar();  
        }
        
        private void SetSoundQuality()
        {
            ReimportClips = SetQuality;
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Sound");
            var soundFiles = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);			
            IterateAudioFiles(soundFiles, "Setting Sound Quality", AudioImportProcessor.SoundQuality);
            EditorUtility.ClearProgressBar();  
        }
        
        private void SetVoiceQuality()
        {
            ReimportClips = SetQuality;
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Voice");
            var voiceFiles = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);			
            IterateAudioFiles(voiceFiles, "Setting Voice Quality", AudioImportProcessor.VoiceQuality);
            EditorUtility.ClearProgressBar();  
        }

        private void SetCompressedInMemory()
        {
            ReimportClips = SetCompressed;
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Sound");
            string[] audioFilePaths = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);			
            IterateAudioFiles(audioFilePaths, "Setting SFX Clips to Compressed in Memory", AudioImportProcessor.SoundQuality);
            EditorUtility.ClearProgressBar();   
        }
        
        private void SetDecompressOnLoad()
        {
            ReimportClips = SetDecompress;
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Sound");
            string[] audioFilePaths = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);			
            IterateAudioFiles(audioFilePaths, "Setting SFX Clips to Decompress on Load", AudioImportProcessor.SoundQuality);
            EditorUtility.ClearProgressBar();   
        }
        
        private void SetStreaming()
        {
            ReimportClips = SetStream;
                        
            EditorUtility.DisplayCancelableProgressBar("Loading", "Fetching audio clips...", 0);
            var path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Music");            
            string[] audioFilePaths = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);
            IterateAudioFiles(audioFilePaths, "Setting Music to Streaming", AudioImportProcessor.MusicQuality);
            
            path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Voice");
            audioFilePaths = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);
            IterateAudioFiles(audioFilePaths, "Setting Voice to Streaming", AudioImportProcessor.VoiceQuality);
            
            path = Path.Combine(Application.dataPath, AudioPathSettings.OriginalsPath, "Sound", "Ambience");
            audioFilePaths = Directory.GetFiles(path, "*.wav", SearchOption.AllDirectories);
            IterateAudioFiles(audioFilePaths, "Setting Ambience to Streaming", AudioImportProcessor.SoundQuality);
            
            EditorUtility.ClearProgressBar();  
        }

        private void IterateAudioFiles(string[] audioFilePaths, string progressBarTitle, float quality)
        {
            var total = 0;
            foreach (var audioFilePath in audioFilePaths)
            {
                total++;                
                var loadPathShort = audioFilePath.Substring(audioFilePath.IndexOf("Assets", StringComparison.Ordinal));
                if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, loadPathShort, total * 1.0f / audioFilePaths.Length)) break;
                ReimportClips(loadPathShort, quality / 100f);                
            }            
        }
		
        private static void SetStream(string clipPath, float quality)
        {
            var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (!importer) return;
            var setting = importer.defaultSampleSettings;
            if (setting.loadType == AudioClipLoadType.Streaming && !importer.preloadAudioData) return;
            setting.loadType = AudioClipLoadType.Streaming;
            setting.quality = quality;
            importer.defaultSampleSettings = setting;
            importer.preloadAudioData = false;            
            AssetDatabase.ImportAsset(clipPath);
        }

        private static void SetCompressed(string clipPath, float quality)
        {
            if (clipPath.Contains("Ambience")) return;
            var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (!importer) return;
            var setting = importer.defaultSampleSettings;
            if (setting.loadType == AudioClipLoadType.CompressedInMemory && !importer.preloadAudioData && importer.loadInBackground) return;
            setting.loadType = AudioClipLoadType.CompressedInMemory;
            setting.quality = quality;
            setting.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            importer.preloadAudioData = false;
            importer.defaultSampleSettings = setting;
            importer.loadInBackground = true;
            AssetDatabase.ImportAsset(clipPath);
        }
        
        private static void SetDecompress(string clipPath, float quality)
        {
            if (clipPath.Contains("Ambience")) return;
            var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (!importer) return;
            var setting = importer.defaultSampleSettings;
            if (setting.loadType == AudioClipLoadType.DecompressOnLoad && !importer.preloadAudioData && importer.loadInBackground) return;
            setting.loadType = AudioClipLoadType.DecompressOnLoad;
            setting.quality = quality;
            setting.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.preloadAudioData = false;
            importer.defaultSampleSettings = setting;
            importer.loadInBackground = true;
            AssetDatabase.ImportAsset(clipPath);
        }
        
        private static void SetQuality(string clipPath, float quality)
        {
            var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
            if (!importer) return;
            var setting = importer.defaultSampleSettings;
            if (Math.Abs(setting.quality - quality) < 0.01f && !importer.preloadAudioData) return;
            setting.quality = quality;
            importer.defaultSampleSettings = setting;
            importer.preloadAudioData = false;
            AssetDatabase.ImportAsset(clipPath);
        }
        #endregion

        #region BatchProcessing        
        private static void SearchFiles<T>(string path, string extension, string progressBarTitle, Action<T> action) where T : ScriptableObject
        {
            try
            {
                var filePaths = Directory.GetFiles(path, extension, SearchOption.AllDirectories);
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var p = filePaths[i];
                    if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, p, i * 1.0f / filePaths.Length)) break;
                    var shortPath = p.Substring(p.IndexOf("Assets", StringComparison.Ordinal));
                    var asset = AssetDatabase.LoadAssetAtPath<T>(shortPath);
                    if (asset) action(asset);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CleanUp(AudioObject ae)
        {
            ae.OnValidate();
            ae.CleanUp();         
            EditorUtility.SetDirty(ae);
        }
        
        private static void CleanUpEmptyFields(bool saveAssets)
        {            
            var searchPath = Path.Combine(Application.dataPath, AudioPathSettings.SoundEventsPath);
            SearchFiles<SoundContainer>(searchPath, "*.asset", "Cleaning Up Sound Events", CleanUp);
                        
            searchPath = Path.Combine(Application.dataPath, AudioPathSettings.MusicEventsPath);
            SearchFiles<MusicContainer>(searchPath, "*.asset", "Cleaning Up Music Events", CleanUp);
                        
            searchPath = Path.Combine(Application.dataPath, AudioPathSettings.VoiceEventsPath);
            SearchFiles<VoiceEvent>(searchPath, "*.asset", "Cleaning Up Voice Events", CleanUp);
                        
            searchPath = Path.Combine(Application.dataPath, AudioPathSettings.SoundBanksPath);
            SearchFiles<SoundBank>(searchPath, "*.asset", "Cleaning Up Sound Banks", CleanUp);
            
            if (saveAssets) AssetDatabase.SaveAssets();	
            EditorUtility.ClearProgressBar();                          
        }       
        
        private static void SetSpatialSettings()
        {                                    
            var searchPath = Path.Combine(Application.dataPath, AudioPathSettings.SoundEventsPath);
            SearchFiles<SoundContainer>(searchPath, "*.asset", "Setting Sound Events", sc =>
            {
                if (sc.IsUpdatePosition && !sc.SpatialSetting) 
                    sc.SpatialSetting = AudioInitSettings.Instance.DefaultSpatialSetting;
            } );            
            AssetDatabase.SaveAssets();            
        }
        #endregion
    }
}