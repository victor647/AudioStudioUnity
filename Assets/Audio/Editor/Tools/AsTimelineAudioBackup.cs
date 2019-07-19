using System;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using UnityEngine.Timeline;

namespace AudioStudio
{
    public class AsTimelineAudioBackup : AsSearchers
    {		
        protected override string DefaultXmlPath {
            get
            {
                return AudioUtility.CombinePath(XmlDocPath, "AudioPlayableAssets.xml");        
            }
        }
        
        private static AsTimelineAudioBackup _instance;
        public static AsTimelineAudioBackup Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = CreateInstance<AsTimelineAudioBackup>();                    
                }
                return _instance;
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Label("This tool searches for timeline assets (.playable files) in the game and export data to an xml file. \n" +
                            "It can also import data from xml and update timeline audio events into the tracks.");

            AudioScriptGUI.DisplaySearchPath(ref SearchPath);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export")) EditorApplication.delayCall += Export;
            if (GUILayout.Button("Import")) EditorApplication.delayCall += Import;
            if (GUILayout.Button("Compare")) EditorApplication.delayCall += Compare;
            if (GUILayout.Button("Open xml")) Process.Start(DefaultXmlPath);
            GUILayout.EndHorizontal();
        }

        #region Export
        private void Export()
        {
            CleanUp();
            CheckoutLocked(DefaultXmlPath);
            var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocPath, "AudioPlayableAssets.xml", ".xml");
            if (string.IsNullOrEmpty(fileName)) return;                        
            FindFiles(ParseTimeline, "Exporting Timeline Assets", "*.playable");			
            AudioUtility.WriteXml(fileName, XRoot);
            EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components in " + EditedCount + " timeline assets!", "OK");
        }

        private void ParseTimeline(string filePath)
        {                        
            var timeline = (TimelineAsset) AssetDatabase.LoadAssetAtPath(filePath, typeof(TimelineAsset));
            if (!timeline) return;
            foreach (var track in timeline.GetOutputTracks())
            {                
                foreach (var clip in track.GetClips())
                {
                    var apa = clip.asset as AudioPlayableAsset;
                    if (apa == null) continue;
                    var xComponent = new XElement("Component");
                    XRoot.Add(WriteComponentToXml(apa, filePath, track.name, clip, xComponent));
                }
            }
            EditedCount++;
        }

        private XElement WriteComponentToXml(AudioPlayableAsset apa, string filePath, string trackName, TimelineClip clip, XElement xComponent)
        {            
            xComponent.SetAttributeValue("Path", Path.GetDirectoryName(filePath));
            xComponent.SetAttributeValue("Asset", Path.GetFileName(filePath));
            xComponent.SetAttributeValue("Track", trackName);
            xComponent.SetAttributeValue("ClipName", clip.displayName);
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("StartTime", (float) clip.start);
            xSettings.SetAttributeValue("Duration", (float) clip.duration);
            xSettings.SetAttributeValue("StopOnEnd", apa.StopOnEnd);
            xComponent.Add(xSettings);
            var xEvents = new XElement("AudioEvents");
            ExportEvents(apa.StartEvents, xEvents, "Start");
            ExportEvents(apa.EndEvents, xEvents, "End");
            xComponent.Add(xEvents);                                                            
            TotalCount++;
            return xComponent;
        }
        #endregion
        
        #region Compare
        private void Compare()
        {            
            var filePath = EditorUtility.OpenFilePanel("Import from", XmlDocPath, "xml");
            if (filePath == null) return;               
            XRoot = XDocument.Load(filePath).Element("Root");            
            ImportComponents(true);                                                
            AsTimelineCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
        #endregion
        
        #region Update
        private XElement FindComponentNode(string fullPath, TimelineClip clip)
        {            
            LoadOrCreateXmlDoc();            			
            var xComponents = XRoot.Descendants("Component");
            foreach (var xComponent in xComponents)
            {
                if (GetFullAssetPath(xComponent) == fullPath)
                {
                    var startTime = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xComponent.Element("Settings"), "StartTime"));
                    var clipName = AudioUtility.GetXmlAttribute(xComponent, "ClipName");
                    var trackName = AudioUtility.GetXmlAttribute(xComponent, "Track");
                    if (startTime == (float) clip.start || clipName == clip.displayName && trackName == clip.parentTrack.name)
                        return xComponent;
                }
            }
            return null;
        }

        public void RevertComponentToXml(string fullPath, TimelineClip clip, AudioPlayableAsset component)
        {                        
            var xComponent = FindComponentNode(fullPath, clip);
            if (xComponent != null)
            {                              
                if (AudioPlayableAssetImporter(component, clip, xComponent))
                {
                    EditorUtility.SetDirty(component);
                }                  
            }
            DestroyImmediate(this);
        }

        public void UpdateComponentNode(string fullPath, TimelineClip clip, AudioPlayableAsset component)
        {
            CheckoutLocked(DefaultXmlPath);
            var trackName = clip.parentTrack.name;
            var xComponent = FindComponentNode(fullPath, clip);            
            if (xComponent != null)
            {                
                xComponent.RemoveAll();                
                WriteComponentToXml(component, fullPath, trackName, clip, xComponent);
            }
            else
            {                				
                xComponent = new XElement("Component");				
                XRoot.Add(WriteComponentToXml(component, fullPath, trackName, clip, xComponent));                
            }
            AudioUtility.WriteXml(DefaultXmlPath, XRoot);
            DestroyImmediate(this);
        }
        
        public void RemoveComponentNode(string fullPath, TimelineClip clip)
        {
            CheckoutLocked(DefaultXmlPath);
            var xComponent = FindComponentNode(fullPath, clip);        
            if (xComponent != null)
            {
                xComponent.Remove();         
                AudioUtility.WriteXml(DefaultXmlPath, XRoot);                              
            }            
            DestroyImmediate(this);
        }
        #endregion
                
        #region Import
        private void Import()
        {
            CleanUp();
            var fileName = EditorUtility.OpenFilePanel("Import from", XmlDocPath, "xml");
            if (string.IsNullOrEmpty(fileName)) return;      
            XRoot = XDocument.Load(fileName).Element("Root");            
            ImportComponents(false);
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success!", "Updated " + EditedCount + " timeline assets out of " + TotalCount, "OK");            
        }

        private void ImportComponents(bool isCompare)
        {
            try
            {                
                var xComponents = XRoot.Descendants("Component").ToList();                
                TotalCount = xComponents.Count;
                var current = 0;
                foreach (var xComponent in xComponents)
                {
                    var fileName = GetFullAssetPath(xComponent);                                        
                    if (isCompare)
                    {                                           
                        var clip = GetTimelineClipFromXml(xComponent, false);
                        if (clip == null)
                        {
                            AsCompareWindow.MissingComponents.Add(xComponent, "Unhandled");
                            continue;
                        }
                        var apa = (AudioPlayableAsset) clip.asset;
                        if (!apa)                        
                            AsCompareWindow.MissingComponents.Add(xComponent, "Unhandled");                        
                        else if (!apa.IsValid())
                            AsCompareWindow.EmptyComponents.Add(xComponent, "Unhandled");
                        else 
                        {
                            var tempComponent = Instantiate(apa);
                            if (AudioPlayableAssetImporter(tempComponent, clip, xComponent))                        
                                AsCompareWindow.ModifiedComponents.Add(xComponent, "Unhandled");     
                            DestroyImmediate(tempComponent, true);
                        } 
                    }
                    else
                    {
                        var clip = GetTimelineClipFromXml(xComponent, true);
                        var apa = (AudioPlayableAsset) clip.asset;
                        if (AudioPlayableAssetImporter(apa, clip, xComponent))
                        {
                            EditorUtility.SetDirty(apa);
                            EditedCount++;   
                        }                            
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying assets", fileName, current * 1.0f / TotalCount)) break;
                    current++;
                }
                
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }            		
        }

        private static bool AudioPlayableAssetImporter(AudioPlayableAsset apa, TimelineClip clip, XElement node)
        {            
            var modified = ImportEvents(ref apa.StartEvents, node, "Start");
            modified |= ImportEvents(ref apa.EndEvents, node, "End");
            modified |= ImportBool(ref apa.StopOnEnd, AudioUtility.GetXmlAttribute(node, "StopOnEnd"));
            var xSettings = node.Element("Settings");
            var start = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xSettings, "StartTime"));
            if (clip.start != start)
            {
                clip.start = start;
                modified = true;
            }
            var duration = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xSettings, "Duration"));
            if (clip.duration != duration)
            {
                clip.duration = duration;
                modified = true;
            }
            return modified;
        }
        
        public static TimelineClip GetClipFromComponent(AudioPlayableAsset component)
        {
            var path = AssetDatabase.GetAssetPath(component);
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (!timeline) return null;
            foreach (var timelineTrack in timeline.GetOutputTracks())
            {                
                foreach (var clip in timelineTrack.GetClips())
                {
                    if (clip.asset == component) return clip;
                }                
            }
            return null;
        }

        private static TimelineClip GetTimelineClipFromXml(XElement xComponent, bool addIfMissing)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(GetFullAssetPath(xComponent));
            if (!timeline) return null;
            var trackName = AudioUtility.GetXmlAttribute(xComponent, "Track");
            var foundTrack = false;
            
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track.name != trackName) continue;
                foundTrack = true;
                var foundClip = false;
                foreach (var clip in track.GetClips())
                {
                    if (clip.displayName != AudioUtility.GetXmlAttribute(xComponent, "ClipName")) continue;
                    foundClip = true;
                    var apa = clip.asset as AudioPlayableAsset;
                    if (apa) return clip;
                }
                if (!foundClip && addIfMissing)
                {                                        
                    return track.CreateClip<AudioPlayableAsset>();                  
                }
            }
            if (!foundTrack && addIfMissing)
            {
                var track = timeline.CreateTrack<PlayableTrack>(null, trackName);
                return track.CreateClip<AudioPlayableAsset>();                    
            }
            return null;            
        }				
        #endregion

        private class AsTimelineCompare : AsCompareWindow
        {               
            public static void ShowWindow()
            {
                var window = (AsTimelineCompare) GetWindow(typeof(AsTimelineCompare));
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare AudioPlayableAssets");
            }

            protected override void DisplayData(string fullPath, XElement node, string status)
            {
                var track = AudioUtility.GetXmlAttribute(node, "Track");
                var clip = AudioUtility.GetXmlAttribute(node, "ClipName");            
                if (GUILayout.Button(track + "/" + clip + ": " + fullPath.Substring(7) + " (" + status + ")", GUI.skin.label))
                {
                    AsXmlInfo.Init(node);
                }
            }
        
            protected override void LocateComponent(XElement node)
            {
#if UNITY_2018_1_OR_NEWER
				EditorApplication.ExecuteMenuItem("Window/Sequencing/Animator");
#else
                EditorApplication.ExecuteMenuItem("Window/Timeline");
#endif
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(GetFullAssetPath(node));
                if (timeline) Selection.activeObject = timeline;                
                //var clip = GetTimelineClipFromXml(node, false);
                //if (clip != null) Selection.activeObject = clip.asset;		                                											
            }

            protected override void SaveComponent(XElement node)
            {                        
                var clip = GetTimelineClipFromXml(node, false);            
                Instance.UpdateComponentNode(GetFullAssetPath(node), clip, (AudioPlayableAsset) clip.asset);
            }

            protected override void RevertComponent(XElement node)
            {
                var clip = GetTimelineClipFromXml(node, true);
                var apa = (AudioPlayableAsset) clip.asset;
                if (AudioPlayableAssetImporter(apa, clip, node))
                {
                    EditorUtility.SetDirty(apa);				
                }            									         
            }

            protected override void RemoveComponent(XElement node)
            {			
                var clip = GetTimelineClipFromXml(node, false);
                if (clip != null)
                {
                    Instance.RemoveComponentNode(GetFullAssetPath(node), clip);
                    EditorUtility.SetDirty(clip.asset);
                    DestroyImmediate(clip.asset, true);                
                }            
            }      
        }
    }        
}