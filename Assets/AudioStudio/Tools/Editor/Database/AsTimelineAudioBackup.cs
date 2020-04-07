using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using AudioStudio.Timeline;
using UnityEngine.Timeline;
using Debug = UnityEngine.Debug;

namespace AudioStudio.Tools
{
    internal class AsTimelineAudioBackup : AsSearchers
    {
        private static AsTimelineAudioBackup _instance;
        internal static AsTimelineAudioBackup Instance
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
            get { return AsScriptingHelper.CombinePath(XmlDocDirectory, "AudioTimelineClip.xml"); }
        }

        #region Export
        internal void Export()
        {
            CleanUp();
            var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocDirectory, "AudioTimelineClip.xml", ".xml");
            if (string.IsNullOrEmpty(fileName)) return;                        
            FindFiles(ParseTimeline, "Exporting Timeline Assets", "*.playable");			
            AsScriptingHelper.WriteXml(fileName, XRoot);
            EditorUtility.DisplayDialog("Process Finished!", "Found " + TotalCount + " components in " + EditedCount + " timeline assets!", "OK");
        }

        internal void ParseTimeline(string assetPath)
        {                        
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline) return;
            var xTimeline = new XElement("Timeline");
            foreach (var track in timeline.GetOutputTracks())
            {     
                if (!(track is AudioTimelineTrack)) continue;
                foreach (var clip in track.GetClips())
                {
                    var asset = clip.asset as AudioTimelineClip;
                    if (asset == null) continue;
                    xTimeline.Add(ParseComponent(asset, track.name, clip));
                }
            }

            if (xTimeline.HasElements)
            {
                xTimeline.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xTimeline);
            }
            EditedCount++;
        }

        private XElement ParseComponent(AudioTimelineClip asset, string trackName, TimelineClip clip)
        {
            var xComponent = new XElement("Component");
            xComponent.SetAttributeValue("TrackName", trackName);
            xComponent.SetAttributeValue("ClipName", clip.displayName);
            AsComponentBackup.AudioTimelineClipExporter(asset, clip, xComponent);
            TotalCount++;
            return xComponent;
        }
        #endregion
        
        #region Import
        internal void Import()
        {
            var xmlPath = EditorUtility.OpenFilePanel("Import from", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(xmlPath) || !ReadData(xmlPath)) return;
            ImportTimelines(false);
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Process Finished!", "Updated " + EditedCount + " timeline assets out of " + TotalCount, "OK");            
        }

        private void ImportTimelines(bool isCompare)
        {
            var xAssets = XRoot.Elements().ToList();
            TotalCount = xAssets.Count;
            try
            {
                for (var i = 0; i < TotalCount; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xAssets[i], "AssetPath");
                    if (!assetPath.Contains(SearchPath)) continue;
                    if (isCompare)
                        CompareTimeline(xAssets[i]);
                    else
                    {
                        if (ImportTimeline(xAssets[i]))
                            EditedCount++;
                    }
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying timeline assets", assetPath, (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            } 
        }

        internal bool ImportTimeline(XElement xTimeline)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xTimeline, "AssetPath");
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline)
            {
                Debug.LogError("Backup Failed: Can't find Timeline asset at " + assetPath);
                return false;
            }
            var modified = false;
            foreach (var xComponent in xTimeline.Elements())
            {
                var clip = GetClipFromXml(timeline, xComponent, true);
                if (clip == null) continue;
                var asset = (AudioTimelineClip) clip.asset;
                if (AsComponentBackup.AudioTimelineClipImporter(asset, clip, xComponent))
                {
                    EditorUtility.SetDirty(asset);
                    modified = true;
                }
            }
            return modified;
        }
        #endregion
        
        #region Compare
        internal void Compare()
        {
            var xmlPath = EditorUtility.OpenFilePanel("Compare with", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(xmlPath) || !ReadData(xmlPath)) return;
            ImportTimelines(true);                                                
            AsTimelineCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();
        }
        
        private void CompareTimeline(XElement xTimeline)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xTimeline, "AssetPath");
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline)
            {
                Debug.LogError("Backup Failed: Can't find Timeline asset at " + assetPath);
                return;
            }
            foreach (var xComponent in xTimeline.Elements())
            {
                var data = new ComponentComparisonData
                {
                    AssetPath = assetPath,
                    ComponentData = xComponent,
                    BackupStatus = ComponentBackupStatus.Unhandled
                };
                var clip = GetClipFromXml(timeline, xComponent, false);
                if (clip == null)
                    AsCompareWindow.MissingComponents.Add(data);
                else
                {
                    var asset = (AudioTimelineClip) clip.asset;
                    if (!asset.IsValid())
                        AsCompareWindow.EmptyComponents.Add(data);
                    else
                    {
                        var tempComponent = Instantiate(asset);
                        if (AsComponentBackup.AudioTimelineClipImporter(tempComponent, clip, xComponent))
                            AsCompareWindow.ModifiedComponents.Add(data);
                        DestroyImmediate(tempComponent, true);
                    }
                }
            }
        }
        #endregion
        
        #region Locate
        private XElement FindAssetNode(string assetPath, bool createIfMissing)
        {
            ReadData();
            var xAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset == null && createIfMissing)
            {
                xAsset = new XElement("Timeline");
                xAsset.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xAsset);
            }
            return xAsset;
        }

        private static XElement FindComponentNode(XElement xAsset, TimelineClip clip)
        {
            var xComponents = xAsset.Elements().Where(x => clip.parentTrack.name == AsScriptingHelper.GetXmlAttribute(x, "TrackName"));
            foreach (var xComponent in xComponents)
            {
                var startTime = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(xComponent.Element("Settings"), "StartTime"));
                var clipName = AsScriptingHelper.GetXmlAttribute(xComponent, "ClipName");
                if (Math.Abs(startTime - clip.start) < 0.01f || clipName == clip.displayName)
                    return xComponent;
            }
            return null;
        }
        
        internal bool ComponentBackedUp(string assetPath, TimelineClip component)
        {
            var xAsset = FindAssetNode(assetPath, false);
            if (xAsset == null) return false;
            return FindComponentNode(xAsset, component) != null;
        }
        
        internal static TimelineClip GetClipFromComponent(string assetPath, AudioTimelineClip component)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline) return null;
            var AudioTracks = timeline.GetOutputTracks().Where(track => track is AudioTimelineTrack);
            return AudioTracks.SelectMany(track => track.GetClips()).FirstOrDefault(clip => clip.asset == component);
        }

        private static TimelineClip GetClipFromXml(TimelineAsset timeline, XElement xComponent, bool addIfMissing)
        {
            var trackName = AsScriptingHelper.GetXmlAttribute(xComponent, "TrackName");
            var track = timeline.GetOutputTracks().Where(t => t is AudioTimelineTrack).FirstOrDefault(t => t.name == trackName);
            if (!track && addIfMissing)
                return timeline.CreateTrack<AudioTimelineTrack>(null, trackName).CreateClip<AudioTimelineClip>();
            
            var startTime = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(xComponent.Element("Settings"), "StartTime"));
            var clip = track.GetClips().FirstOrDefault(c => Math.Abs(c.start - startTime) <= 0.01f);
            if (clip == null)
            {
                var clipName = AsScriptingHelper.GetXmlAttribute(xComponent, "ClipName");
                clip = track.GetClips().FirstOrDefault(c => c.displayName == clipName);
            }
            return clip ?? (addIfMissing ? track.CreateClip<AudioTimelineClip>() : null);
        }
        #endregion
        
        #region Update
        internal bool UpdateXmlFromComponent(string assetPath, TimelineClip clip, AudioTimelineClip component)
        {
            var trackName = clip.parentTrack.name;
            var xAsset = FindAssetNode(assetPath, true);
            var xComponent = FindComponentNode(xAsset, clip);            
            if (xComponent != null)
            {
                var xTemp = ParseComponent(component, trackName, clip);
                if (XNode.DeepEquals(xTemp, xComponent)) return false;
                xComponent.ReplaceWith(xTemp);    
            }
            else
                xAsset.Add(ParseComponent(component, trackName, clip));
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
            return true;
        }
        #endregion

        #region Revert
        internal bool RevertComponentToXml(string assetPath, TimelineClip clip, AudioTimelineClip component)
        {                      
            var xAsset = FindAssetNode(assetPath, true);
            var xComponent = FindComponentNode(xAsset, clip);
            return xComponent != null ? AsComponentBackup.AudioTimelineClipImporter(component, clip, xComponent) : UpdateXmlFromComponent(assetPath, clip, component);
        }
        #endregion
        
        #region Remove
        internal void RemoveUnsaved()
        {
            CleanUp();
            FindFiles(RemoveUnsavedInTimeline, "Removing Unsaved Timeline Assets", "*.playable");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Process Finished!", "Removed " + EditedCount + " audio tracks in " + TotalCount + " timeline assets!", "OK");
        }
        
        internal void RemoveAll()
        {
            CleanUp();
            FindFiles(RemoveAllInTimeline, "Removing Timeline Assets", "*.playable");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Process Finished!", "Removed " + EditedCount + " audio tracks in " + TotalCount + " timeline assets!", "OK");
        }

        internal void RemoveUnsavedInTimeline(string assetPath)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline) return;

            var audioTracks = timeline.GetOutputTracks().Where(track => track is AudioTimelineTrack);
            foreach (var track in audioTracks)
            {
                foreach (var clip in track.GetClips())
                {
                    if (!ComponentBackedUp(assetPath, clip))
                        DestroyImmediate(clip.asset);
                }
                EditedCount++;
                EditorUtility.SetDirty(timeline);
            }
            TotalCount++;
        }
        
        internal void RemoveAllInTimeline(string assetPath)
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
            if (!timeline) return;
            var xAsset = FindAssetNode(assetPath, false);
            if (xAsset != null)
                xAsset.Remove();
            
            var toBeDeleted = timeline.GetOutputTracks().Where(track => track is AudioTimelineTrack);
            foreach (var track in toBeDeleted)
            {
                timeline.DeleteTrack(track);
                EditedCount++;
                EditorUtility.SetDirty(timeline);
            }
            TotalCount++;
        }
        
        // remove node from component inspector
        internal void RemoveComponentXml(string assetPath, TimelineClip clip)
        {
            ReadData();
            var xAsset = FindAssetNode(assetPath, false);
            if (xAsset == null) return;
            var xComponent = FindComponentNode(xAsset, clip);
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }

        // remove node from compare window
        private void RemoveComponentXml(ComponentComparisonData data)
        {
            var xAsset = FindAssetNode(data.AssetPath, false);
            var xComponent = xAsset.Elements().FirstOrDefault(x => XNode.DeepEquals(x, data.ComponentData));
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        #endregion

        private class AsTimelineCompare : AsCompareWindow
        {               
            internal static void ShowWindow()
            {
                var window = GetWindow<AsTimelineCompare>();
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare AudioPlayableAssets");
            }

            protected override void DisplayData(ComponentComparisonData data)
            {
                var track = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "TrackName");
                var clip = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "ClipName");
                if (GUILayout.Button(string.Format("{0}/{1}: {2} ({3})", track, clip, Path.GetFileNameWithoutExtension(data.AssetPath), data.BackupStatus), GUI.skin.label))
                    AsXmlInfo.Init(data.ComponentData);
            }
        
            protected override void LocateComponent(ComponentComparisonData data)
            {
#if UNITY_2018_1_OR_NEWER
				EditorApplication.ExecuteMenuItem("Window/Sequencing/Animator");
#else
                EditorApplication.ExecuteMenuItem("Window/Timeline");
#endif
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(data.AssetPath);
                if (timeline) Selection.activeObject = timeline;
            }

            protected override void RemoveComponent(ComponentComparisonData data)
            {			
                Instance.RemoveComponentXml(data);
            }      
        }
    }        
}