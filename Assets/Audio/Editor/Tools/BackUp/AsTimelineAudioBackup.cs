using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using UnityEngine.Timeline;
using AudioPlayableAsset = AudioStudio.Components.AudioPlayableAsset;

namespace AudioStudio.Tools
{
    public class AsTimelineAudioBackup : AsSearchers
    {
        private static AsTimelineAudioBackup _instance;
        public static AsTimelineAudioBackup Instance
        {
            get
            {
                if (!_instance)
                    _instance = CreateInstance<AsTimelineAudioBackup>();
                return _instance;
            }
        }

        protected override string DefaultXmlPath 
        {
            get { return AudioUtility.CombinePath(XmlDocPath, "AudioPlayableAssets.xml"); }
        }
        
        #region Export
        protected internal void Export()
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
        protected internal void Compare()
        {       
            CleanUp();
            var filePath = EditorUtility.OpenFilePanel("Compare from", XmlDocPath, "xml");
            if (string.IsNullOrEmpty(filePath)) return;               
            XRoot = XDocument.Load(filePath).Element("Root");            
            ImportComponents(true);                                                
            AsTimelineCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();
        }
        #endregion
        
        #region Remove
        protected internal void RemoveAll()
        {
            CleanUp();
            FindFiles(RemoveTimeline, "Removing Timeline Assets", "*.playable");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success!", "Removed " + EditedCount + " audio tracks in " + TotalCount + " timeline assets!", "OK");
        }
        
        private void RemoveTimeline(string filePath)
        {                        
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(filePath);
            if (!timeline) return;
            var toBeDeleted = timeline.GetOutputTracks().Where(track => track.GetClips().Any(clip => clip.asset is AudioPlayableAsset));

            foreach (var track in toBeDeleted)
            {
                timeline.DeleteTrack(track);
                EditedCount++;
                EditorUtility.SetDirty(timeline);
            }
            TotalCount++;
        }
        #endregion
        
        #region Rename
        protected internal void Rename()
        {
            CleanUp();
            FindFiles(RenameTimeline, "Renaming Timeline Assets", "*.playable");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success!", "Renamed " + EditedCount + " audio tracks and clips in " + TotalCount + " timeline assets!", "OK");
        }
        
        private void RenameTimeline(string filePath)
        {                        
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(filePath);
            if (!timeline) return;
            var edited = false;
            foreach (var track in timeline.GetOutputTracks())
            {
                var isAudioTrack = false;
                foreach (var clip in track.GetClips())
                {
                    var apa = clip.asset as AudioPlayableAsset;
                    if (apa)
                    {
                        isAudioTrack = true;
                        if (clip.displayName == "AudioPlayableAsset")
                        {
                            clip.displayName = apa.AutoRename();
                            edited = true;
                            EditedCount++;
                        }
                    }

                    if (isAudioTrack && track.name.Contains("Playable Track"))
                    {
                        track.name = track.name.Replace("Playable Track", "Audio");
                        EditedCount++;
                        edited = true;
                    }
                }
            }
            TotalCount++;
            if (edited)
                EditorUtility.SetDirty(timeline);
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

        public bool RevertComponentToXml(string fullPath, TimelineClip clip, AudioPlayableAsset component)
        {                        
            var xComponent = FindComponentNode(fullPath, clip);
            if (xComponent != null)
            {                              
                if (AudioPlayableAssetImporter(component, clip, xComponent))
                {
                    EditorUtility.SetDirty(component);
                    return true;
                }
                return false;
            }
            return UpdateComponentNode(fullPath, clip, component);
        }

        public bool UpdateComponentNode(string fullPath, TimelineClip clip, AudioPlayableAsset component)
        {
            CheckoutLocked(DefaultXmlPath);
            var trackName = clip.parentTrack.name;
            var xComponent = FindComponentNode(fullPath, clip);            
            if (xComponent != null)
            {                
                var xTemp = new XElement("Component");           
                WriteComponentToXml(component, fullPath, trackName, clip, xTemp);
                if (XNode.DeepEquals(xTemp, xComponent)) return false;
                xComponent.ReplaceWith(xTemp);    
            }
            else
            {                				
                xComponent = new XElement("Component");				
                XRoot.Add(WriteComponentToXml(component, fullPath, trackName, clip, xComponent));                
            }
            AudioUtility.WriteXml(DefaultXmlPath, XRoot);
            return true;
        }
        
        protected internal void RemoveComponentNode(string fullPath, TimelineClip clip)
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
        protected internal void Import()
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
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }            		
        }

        private static bool AudioPlayableAssetImporter(AudioPlayableAsset apa, TimelineClip clip, XElement node)
        {            
                     
            var xSettings = node.Element("Settings");
            var clipName = AudioUtility.GetXmlAttribute(node, "ClipName");
            var modified = false;
            var start = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xSettings, "StartTime"));
            if (clip.displayName != clipName)
            {
                clip.displayName = clipName;
                modified = true;
            }
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
            modified |= ImportEvents(ref apa.StartEvents, node, "Start");
            modified |= ImportEvents(ref apa.EndEvents, node, "End");
            return modified;
        }
        
        public static TimelineClip GetClipFromComponent(AudioPlayableAsset component)
        {
            var path = AssetDatabase.GetAssetPath(component);
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            return !timeline ? null : timeline.GetOutputTracks().SelectMany(track => track.GetClips()).FirstOrDefault(clip => clip.asset == component);
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
                var window = GetWindow<AsTimelineCompare>();
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