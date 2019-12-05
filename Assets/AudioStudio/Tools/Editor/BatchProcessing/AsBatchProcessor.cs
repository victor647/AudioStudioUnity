using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Tools
{
    public enum AudioFileFormat
    {
        wav,
        mp3,
        ogg,
        aiff
    }
    
    public class AsBatchProcessor : EditorWindow
    {		
        private Platform _platform;
        private Languages _language;
        private AudioFileFormat _audioFileFormat = AudioFileFormat.wav;
        private string _audioFileExtension => "." + _audioFileFormat;
        
        #region GUI
        private void OnGUI()
        {                    
            DrawObjectsGenerator();
            DrawImportSettings(); 
            DrawOtherTools();
            GUI.contentColor = Color.yellow;
            if (GUILayout.Button("ONE CLICK REFRESH", EditorStyles.toolbarButton))
            {
                SetMusicQuality();                     
                GenerateSoundClips(false);
                GenerateSoundContainers(false);
                GenerateMusicTracks(false);
                GenerateMusicContainers(false);
                GenerateVoiceEvents(false);
                VerifyConfigs();                                                                                           
            }
        }
        
        private void DrawObjectsGenerator()
        {
            EditorGUILayout.LabelField("Object Generator", EditorStyles.boldLabel); 
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Target Platform");  
                _platform = (Platform) EditorGUILayout.EnumPopup(_platform);
                GUILayout.EndHorizontal();
            
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Original Audio File Format");  
                _audioFileFormat = (AudioFileFormat) EditorGUILayout.EnumPopup(_audioFileFormat);
                GUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField("Sound:");  
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Single Clips")) GenerateSoundClips(true);
                if (GUILayout.Button("Multi Containers")) GenerateSoundContainers(true);                
                GUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField("Music:");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Single Tracks")) GenerateMusicTracks(true);
                if (GUILayout.Button("Multi Containers")) GenerateMusicContainers(true);
                if (GUILayout.Button("Playable Instruments")) GenerateMusicInstruments(true);
                GUILayout.EndHorizontal();  
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Voice:");
                EditorGUILayout.LabelField("Language");
                _language = (Languages) EditorGUILayout.EnumPopup(_language);
                GUILayout.EndHorizontal();   
                if (GUILayout.Button("All Voice Events")) GenerateVoiceEvents(true);
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
                GUILayout.Label("Streaming Duration Threshold");
                AudioImportProcessor.StreamDurationThreshold = EditorGUILayout.IntSlider(AudioImportProcessor.StreamDurationThreshold, 3, 10);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawQuality(string label, ref int quality, Action setQuality)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " Quality");
            quality = EditorGUILayout.IntSlider(quality, 10, 100);
            if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(50))) setQuality();
            GUILayout.EndHorizontal(); 
        }

        private void DrawOtherTools()
        {
            EditorGUILayout.LabelField("Miscellaneous Tools", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUILayout.Button("Verify All Configs")) VerifyConfigs();
                if (GUILayout.Button("Delete Empty Configs")) DeleteEmptyConfigs();
            }
        }
        #endregion
		
        #region GenerateAssets
        private void GenerateMusicTracks(bool saveAssets)
        {
            var sourceFolder = AsScriptingHelper.CombinePath(_platform == Platform.Web ? AsPathSettings.StreamingClipsPath : AsPathSettings.OriginalsPath, "Music");

            string[] audioFilePaths = Directory.GetFiles(AsScriptingHelper.CombinePath(Application.dataPath, sourceFolder), "*" + _audioFileExtension, SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {
                var savePathLong = audioFilePaths[i].Replace(sourceFolder, AsPathSettings.MusicEventsPath).Replace(_audioFileExtension, ".asset").Replace("Music_", "");
                var loadPathShort = AsScriptingHelper.ShortPath(audioFilePaths[i]);
                var savePathShort = AsScriptingHelper.ShortPath(savePathLong);              
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
                AsScriptingHelper.CheckDirectoryExist(savePathLong);												                
                var newTrack = CreateInstance<MusicTrack>();
                newTrack.name = clip.name.Substring(6);                
                if (_platform == Platform.Web)
                    newTrack.Platform = Platform.Web;
                else
                    newTrack.Clip = clip;    
                AssetDatabase.CreateAsset(newTrack, savePathShort);
            }		
            if (saveAssets) 
                AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();   
        }
        
        private void GenerateSoundClips(bool saveAssets)
        {
            var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.OriginalsPath, "Sound");
            string[] audioFilePaths = Directory.GetFiles(searchPath, "*" + _audioFileExtension, SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {				
                var savePathLong = audioFilePaths[i].Replace(AsScriptingHelper.CombinePath(AsPathSettings.OriginalsPath, "Sound"), AsPathSettings.SoundEventsPath).Replace(_audioFileExtension, ".asset");				
                var loadPathShort = AsScriptingHelper.ShortPath(audioFilePaths[i]);
                var savePathShort = AsScriptingHelper.ShortPath(savePathLong);
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
                AsScriptingHelper.CheckDirectoryExist(savePathLong);				
                var newClip = CreateInstance<SoundClip>();	                
                newClip.name = clip.name;				
                newClip.Clip = clip;                
                AssetDatabase.CreateAsset(newClip, savePathShort);	
            }		
            if (saveAssets) 
                AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();    
        }

        private void GenerateSoundContainers(bool saveAssets)
        {
            var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.SoundEventsPath);
            string[] soundClipPaths = Directory.GetFiles(searchPath, "*.asset", SearchOption.AllDirectories);
            for (var i = 0; i < soundClipPaths.Length; i++)
            {				                			
                var loadPathShort = AsScriptingHelper.ShortPath(soundClipPaths[i]);
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
                        AsScriptingHelper.CheckDirectoryExist(savePathLong);						
                        var eventName = Path.GetFileName(eventNameLong);                                                    
                        var newRandomEvent = CreateInstance<SoundContainer>();                        
                        newRandomEvent.PlayLogic = SoundPlayLogic.Random;
                        newRandomEvent.name = eventName;				
                        newRandomEvent.ChildEvents.Add(clip);
                        AssetDatabase.CreateAsset(newRandomEvent, eventNameShort + ".asset");
                    }                    
                }	                                
            }		
            if (saveAssets) 
                AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();   
        }
        
        private void GenerateMusicContainers(bool saveAssets)
        {
            var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.MusicEventsPath);
            string[] musicTrackPaths = Directory.GetFiles(searchPath, "*.asset", SearchOption.AllDirectories);
            for (var i = 0; i < musicTrackPaths.Length; i++)
            {                	
                var loadPathShort = AsScriptingHelper.ShortPath(musicTrackPaths[i]);            
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
            if (saveAssets) 
                AssetDatabase.SaveAssets();
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
            AsScriptingHelper.CheckDirectoryExist(savePathLong);						
            var eventName = Path.GetFileName(eventPathLong);                                                    
            var newMusicContainer = CreateInstance<MusicContainer>();
            newMusicContainer.PlayLogic = playLogic;
            newMusicContainer.name = eventName;				
            newMusicContainer.ChildEvents.Add(track);                                                                    
            AssetDatabase.CreateAsset(newMusicContainer, eventPathShort + ".asset");
        }
        
        /*
         * Naming rule:
         * InstrumentName_LowNote(optional)-CenterNote-HighNote(optional)_RandomNumber(optional)
         *  Example: Guitar_59-60-61_01 or as simple as Piano_72
         */
        private void GenerateMusicInstruments(bool saveAssets)
        {
            try
            {
                var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.OriginalsPath, "Instruments");
                string[] samplePaths = Directory.GetFiles(searchPath, "*" + _audioFileExtension, SearchOption.AllDirectories);
                for (var i = 0; i < samplePaths.Length; i++)
                {
                    var loadPathShort = AsScriptingHelper.ShortPath(samplePaths[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Generating Music Instruments", loadPathShort, (i + 1) * 1.0f / samplePaths.Length)) break;

                    var sample = AssetDatabase.LoadAssetAtPath<AudioClip>(loadPathShort);
                    var properties = Path.GetFileNameWithoutExtension(loadPathShort).Split('_');
                    if (properties.Length < 2) continue; //not matching naming rule
                    var instrumentName = properties[0];
                    var savePath = $"Assets/{AsPathSettings.MusicInstrumentsPath}/{instrumentName}.asset";
                    var instrument = AssetDatabase.LoadAssetAtPath<MusicInstrument>(savePath);
                    if (!instrument)
                    {
                        instrument = CreateInstance<MusicInstrument>();
                        AssetDatabase.CreateAsset(instrument, savePath);
                    }

                    var noteRanges = properties[1].Split('-');
                    var centerNote = byte.Parse(noteRanges[noteRanges.Length == 3 ? 1 : 0]);
                    var mapping = instrument.GetMappingByCenterNote(centerNote);
                    mapping.CenterNote = centerNote;
                    if (noteRanges.Length == 3)
                    {
                        mapping.MultiNote = true;
                        mapping.LowestNote = byte.Parse(noteRanges[0]);
                        mapping.HighestNote = byte.Parse(noteRanges[2]);
                    }

                    if (!mapping.Samples.Contains(sample))
                        AsScriptingHelper.AddToArray(ref mapping.Samples, sample);
                    EditorUtility.SetDirty(instrument);
                }

                if (saveAssets) 
                    AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }
        
        private void GenerateVoiceEvents(bool saveAssets)
        {
            string sourceFolder;
            sourceFolder = AsScriptingHelper.CombinePath(_platform == Platform.Web ? AsPathSettings.StreamingClipsPath : AsPathSettings.OriginalsPath, "Voice", _language.ToString());

            var destinationFolder = AsScriptingHelper.CombinePath(AsPathSettings.VoiceEventsPath, _language.ToString());
            
            string[] audioFilePaths = Directory.GetFiles(AsScriptingHelper.CombinePath(Application.dataPath, sourceFolder), "*" + _audioFileExtension, SearchOption.AllDirectories);			
            for (var i = 0; i < audioFilePaths.Length; i++)
            {
                var savePathLong = audioFilePaths[i].Replace(sourceFolder, destinationFolder).Replace(_audioFileExtension, ".asset").Replace("Vo_", "");				
                var loadPathShort = AsScriptingHelper.ShortPath(audioFilePaths[i]);
                var savePathShort = AsScriptingHelper.ShortPath(savePathLong);
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
                        AsScriptingHelper.CheckDirectoryExist(savePathLong);						                                                                          
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
                AsScriptingHelper.CheckDirectoryExist(savePathLong);				
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
            var path = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.OriginalsPath, "Music");
            var musicFiles = Directory.GetFiles(path, "*" + _audioFileExtension, SearchOption.AllDirectories);			
            IterateAudioFiles(musicFiles, "Setting Music Quality", AudioImportProcessor.MusicQuality);
            EditorUtility.ClearProgressBar();  
        }
        
        private void SetSoundQuality()
        {
            ReimportClips = SetQuality;
            var path = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.OriginalsPath, "Sound");
            var soundFiles = Directory.GetFiles(path, "*" + _audioFileExtension, SearchOption.AllDirectories);			
            IterateAudioFiles(soundFiles, "Setting Sound Quality", AudioImportProcessor.SoundQuality);
            EditorUtility.ClearProgressBar();  
        }
        
        private void SetVoiceQuality()
        {
            ReimportClips = SetQuality;
            var path = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.OriginalsPath, "Voice");
            var voiceFiles = Directory.GetFiles(path, "*" + _audioFileExtension, SearchOption.AllDirectories);			
            IterateAudioFiles(voiceFiles, "Setting Voice Quality", AudioImportProcessor.VoiceQuality);
            EditorUtility.ClearProgressBar();  
        }

        private void IterateAudioFiles(string[] audioFilePaths, string progressBarTitle, float quality)
        {
            var total = 0;
            foreach (var audioFilePath in audioFilePaths)
            {
                total++;                
                var loadPathShort = AsScriptingHelper.ShortPath(audioFilePath);
                if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, loadPathShort, total * 1.0f / audioFilePaths.Length)) break;
                ReimportClips(loadPathShort, quality / 100f);                
            }            
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
        private static void SearchFiles<T>(string path, string _audioFileFormat, string progressBarTitle, Action<T> action) where T : ScriptableObject
        {
            try
            {
                var filePaths = Directory.GetFiles(path, _audioFileFormat, SearchOption.AllDirectories);
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var shortPath = AsScriptingHelper.ShortPath(filePaths[i]);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(shortPath);
                    if (asset) action(asset);
                    if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, filePaths[i], i * 1.0f / filePaths.Length)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CleanUp(AudioConfig config)
        {
            config.OnValidate();
            config.CleanUp();         
            EditorUtility.SetDirty(config);
        }
        
        private static void Delete(AudioConfig config)
        {
            if (!config.IsValid())
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(config));
        }
        
        private static void DeleteEmptyConfigs()
        {
            var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.SoundEventsPath);
            SearchFiles<SoundContainer>(searchPath, "*.asset", "Cleaning Up Sound Events", Delete);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.MusicEventsPath);
            SearchFiles<MusicContainer>(searchPath, "*.asset", "Cleaning Up Music Events", Delete);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.VoiceEventsPath);
            SearchFiles<VoiceEvent>(searchPath, "*.asset", "Cleaning Up Voice Events", Delete);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.SoundBanksPath);
            SearchFiles<SoundBank>(searchPath, "*.asset", "Cleaning Up Sound Banks", Delete);
            EditorUtility.ClearProgressBar();                          
        } 
        
        private static void VerifyConfigs()
        {
            var searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.SoundEventsPath);
            SearchFiles<SoundContainer>(searchPath, "*.asset", "Checking Sound Events", CleanUp);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.MusicEventsPath);
            SearchFiles<MusicContainer>(searchPath, "*.asset", "Checking Music Events", CleanUp);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.VoiceEventsPath);
            SearchFiles<VoiceEvent>(searchPath, "*.asset", "Checking Voice Events", CleanUp);
                        
            searchPath = AsScriptingHelper.CombinePath(Application.dataPath, AsPathSettings.SoundBanksPath);
            SearchFiles<SoundBank>(searchPath, "*.asset", "Checking Sound Banks", CleanUp);
            
            AssetDatabase.SaveAssets();	
            EditorUtility.ClearProgressBar();                          
        }
        #endregion
    }
}