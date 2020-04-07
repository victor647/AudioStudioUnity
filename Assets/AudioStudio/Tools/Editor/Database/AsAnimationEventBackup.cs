using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using AudioStudio.Components;
using Debug = UnityEngine.Debug;

namespace AudioStudio.Tools
{
    internal class AsAnimationEventBackup : AsSearchers
    {
        #region Fields               
        internal bool IncludeNonAudioEvents;
		
        private static AsAnimationEventBackup _instance;
        internal static AsAnimationEventBackup Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = CreateInstance<AsAnimationEventBackup>();                                       
                }
                return _instance;
            }
        }

        protected override string DefaultXmlPath {
            get
            {
                return AsScriptingHelper.CombinePath(XmlDocDirectory, "AnimationEvents.xml");        
            }
        }
        #endregion

        #region Export
        internal void Export()
        {
            CleanUp();
            var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocDirectory, "AnimationEvents.xml", "xml");
            if (string.IsNullOrEmpty(fileName)) return;			
            if (IncludeA) FindFiles(ParseAnimation, "Exporting animation clips...", "*.anim");
            if (IncludeB) FindFiles(ParseModel, "Exporting FBX files...", "*.fbx");
            AsScriptingHelper.WriteXml(fileName, XRoot);
            EditorUtility.DisplayDialog("Process Finished!", "Found " + TotalCount + " animation events in " + EditedCount + " clips and models!", "OK");		
        }

        internal void ParseAnimation(string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip == null) return;
            var xClip = ParseClip(clip, assetPath);
            if (xClip.HasElements)
            {
                XRoot.Add(xClip);
                EditedCount++;
            }
        }

        private XElement ParseClip(AnimationClip clip, string assetPath)
        {
            var xClip = new XElement("Animation");
            xClip.SetAttributeValue("AssetPath", assetPath);
            foreach (var evt in clip.events)
            {
                if (IncludeNonAudioEvents || IsSoundAnimationEvent(evt))
                    xClip.Add(ParseEvent(evt));
            }
            return xClip;
        }

        internal void ParseModel(string assetPath)
        {
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null) return;		
			
            var xModel = new XElement("Model");
            foreach (var clip in modelImporter.clipAnimations)
            {
                var xClip = ParseModelClip(clip);
                if (xClip.HasElements)
                {
                    xModel.Add(xClip);
                    EditedCount++;
                }
            }

            if (xModel.HasElements)
            {
                xModel.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xModel);
            }
        }
		
        private XElement ParseModelClip(ModelImporterClipAnimation clip)
        {
            var xClip = new XElement("Clip");
            xClip.SetAttributeValue("Name", clip.name);
            foreach (var evt in clip.events)
            {
                if (IncludeNonAudioEvents || IsSoundAnimationEvent(evt))
                    xClip.Add(ParseEvent(evt));
            }	
            return xClip;
        }
		
        private XElement ParseEvent(AnimationEvent evt)
        {
            TotalCount++;
            var ae = new XElement("AnimationEvent");
            ae.SetAttributeValue("Function", evt.functionName);
            ae.SetAttributeValue("AudioEvent", evt.stringParameter);
            ae.SetAttributeValue("Time", evt.time);
            return ae;
        }
        #endregion
		
        #region Import
        internal void Import()
        {
            var fileName = EditorUtility.OpenFilePanel("Import from", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(fileName) || !ReadData(fileName)) return;
            if (IncludeA) ImportClips(false);
            if (IncludeB) ImportModels(false);
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Process Finished!", "Updated " + EditedCount + " animation clips out of " + TotalCount, "OK");			
        }

        private void ImportClips(bool isCompare)
        {
            var xClips = XRoot.Elements("Animation").ToList();												
            TotalCount += xClips.Count;
            try
            {
                for (var i = 0; i < xClips.Count; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xClips[i], "AssetPath");
                    if (!assetPath.Contains(SearchPath)) continue;
                    if (isCompare)
                        CompareAnimationClip(xClips[i]);
                    else
                        ImportClip(xClips[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying clips", assetPath , (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }		
		
        private void ImportModels(bool isCompare)
        {
            var xModels = XRoot.Elements("Model").ToList();						
            TotalCount += xModels.Count;
            try
            {
                for (var i = 0; i < xModels.Count; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xModels[i], "AssetPath");
                    if (!assetPath.Contains(SearchPath)) continue;
                    if (isCompare)
                        CompareModel(xModels[i]);
                    else
                        ImportModel(xModels[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying models", assetPath , (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }

        internal bool ImportModel(XElement xModel)
        {			
            var assetPath = AsScriptingHelper.GetXmlAttribute(xModel, "AssetPath");
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null) return false;
			
            var modified = false;
            var newClips = new List<ModelImporterClipAnimation>(); 
            foreach (var clip in modelImporter.clipAnimations)
            {
                foreach (var xClip in xModel.Elements())
                {
                    if (AsScriptingHelper.GetXmlAttribute(xClip, "Name") != clip.name) continue;
                    if (ImportModelClip(clip, xClip.Elements("AnimationEvent"))) modified = true;
                }					
                newClips.Add(clip);
            }

            if (modified)
            {
                modelImporter.clipAnimations = newClips.ToArray();
                EditorUtility.SetDirty(modelImporter);
                AssetDatabase.ImportAsset(assetPath);
            }
            return modified;
        }

        internal bool ImportClip(XElement xClip)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xClip, "AssetPath");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (!clip) return false;
            var modified = false;
            var xEvents = xClip.Elements("AnimationEvent");
            var events = IncludeNonAudioEvents ? new List<AnimationEvent>() : clip.events.Where(evt => !IsSoundAnimationEvent(evt)).ToList();                        	                                    
            ImportEvents(events, xEvents);
            if (events.Count != clip.events.Length)
            {
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                modified = true;
            }
            events = events.OrderBy(e => e.time).ToList();
            if (events.Where((t, i) => !CompareAnimationEvent(clip.events[i], t)).Any())
            {
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
                modified = true;
            }
            return modified;
        }

        private bool ImportModelClip(ModelImporterClipAnimation clip, IEnumerable<XElement> xEvents)
        {
            var events = IncludeNonAudioEvents ? new List<AnimationEvent>() : clip.events.Where(evt => !IsSoundAnimationEvent(evt)).ToList();
            ImportEvents(events, xEvents);
            if (events.Count != clip.events.Length)
            {
                EditedCount++;
                clip.events = events.ToArray();
                return true;
            }		
            events = events.OrderBy(e => e.time).ToList();
            if (events.Where((t, i) => !CompareAnimationEvent(clip.events[i], t)).Any())
            {
                EditedCount++;
                clip.events = events.ToArray();
                return true;
            }
            return false;
        }

        private void ImportEvents(ICollection<AnimationEvent> events, IEnumerable<XElement> xEvents)
        {
            foreach (var e in xEvents)
            {
                var animEvent = new AnimationEvent
                {
                    time = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(e, "Time")),
                    stringParameter = AsScriptingHelper.GetXmlAttribute(e, "AudioEvent"),
                    functionName = AsScriptingHelper.GetXmlAttribute(e, "Function")
                };
                events.Add(animEvent);
            }
        }

        private static bool IsSoundAnimationEvent(AnimationEvent animationEvent)
        {
            return typeof(AnimationSound).GetMethods().Where(method => method.IsPublic).Any(method => animationEvent.functionName == method.Name);
        }
        #endregion
		
        #region Compare
        internal void Compare()
        {            
            var xmlPath = EditorUtility.OpenFilePanel("Compare with", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(xmlPath) || !ReadData(xmlPath)) return;
            if (IncludeA) ImportClips(true);            
            if (IncludeB) ImportModels(true);
            AsAnimationEventCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
		
        private void CompareModel(XElement xModel)
        {			
            var assetPath = AsScriptingHelper.GetXmlAttribute(xModel, "AssetPath");
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null) return;
			
            foreach (var clip in modelImporter.clipAnimations)
            {
                foreach (var xClip in xModel.Elements())
                {
                    if (AsScriptingHelper.GetXmlAttribute(xClip, "Name") != clip.name) continue;
                    CompareClip(assetPath, xClip, clip.events);
                }
            }
        }
        
        private void CompareAnimationClip(XElement xClip)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xClip, "AssetPath");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            CompareClip(assetPath, xClip, clip.events);                           
        }

        private void CompareClip(string assetPath, XElement xClip, AnimationEvent[] clipEvents)
        {
            var xEvents = xClip.Elements("AnimationEvent");
            var events = IncludeNonAudioEvents ? new List<AnimationEvent>() : clipEvents.Where(evt => !IsSoundAnimationEvent(evt)).ToList();  
            ImportEvents(events, xEvents);
            var data = new ComponentComparisonData
            {
                AssetPath = assetPath,
                ComponentData = xClip,
                BackupStatus = ComponentBackupStatus.Unhandled
            };
            if (events.Count > clipEvents.Length && clipEvents.Length == 0)
                AsCompareWindow.MissingComponents.Add(data);
            else
            {
                events = events.OrderBy(e => e.time).ToList();
                if (events.Where((t, i) => !CompareAnimationEvent(clipEvents[i], t)).Any())
                    AsCompareWindow.ModifiedComponents.Add(data);
            }
        }
		
        private static bool CompareAnimationEvent(AnimationEvent a, AnimationEvent b)
        {			
            return Math.Abs(a.time - b.time) < 0.01f && a.stringParameter == b.stringParameter && a.functionName == b.functionName;
        }
        #endregion
		
        #region Locate
        private XElement FindAssetNode(string assetPath, bool createIfMissing)
        {
            ReadData();
            var nodeName = assetPath.EndsWith(".anim") ? "Animation" : "Model";
            var xAsset = XRoot.Elements(nodeName).FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset == null && createIfMissing)
            {
                xAsset = new XElement(nodeName);
                xAsset.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xAsset);
            }
            return xAsset;
        }
		
        private XElement FindClipNode(XElement xModel, string clipName)
        {
            return xModel.Elements().FirstOrDefault(x => AsScriptingHelper.GetXmlAttribute(x, "Name") == clipName);
        }
        #endregion

        #region Update

        private ModelImporterClipAnimation GetModelClipFromXml(string assetPath, XElement xClip)
        {
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (!modelImporter) return null;
            var clipName = AsScriptingHelper.GetXmlAttribute(xClip, "Name");
            return modelImporter.clipAnimations.FirstOrDefault(clip => clip.name == clipName);
        }

        private void UpdateXmlFromClip(ComponentComparisonData data)
        {
            var xAsset = FindAssetNode(data.AssetPath, true);
            var clipName = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Name");
            if (string.IsNullOrEmpty(clipName))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(data.AssetPath);
                var xTemp = ParseClip(clip, data.AssetPath);
                if (XNode.DeepEquals(xTemp, xAsset)) return;
                xAsset.ReplaceWith(xTemp);    
            }
            else
            {
                var xClip = FindClipNode(xAsset, clipName);
                var clip = GetModelClipFromXml(data.AssetPath, xClip);
                var xTemp = ParseModelClip(clip);
                if (XNode.DeepEquals(xTemp, xClip)) return;
                xClip.ReplaceWith(xTemp);    
            }
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        #endregion

        #region Revert
        private void RevertClipToXml(ComponentComparisonData data)
        {
            var xAsset = FindAssetNode(data.AssetPath, true);
            var clipName = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Name");
            if (string.IsNullOrEmpty(clipName))
                ImportClip(xAsset);
            else
                ImportModel(xAsset);
        }
        #endregion
		
        #region Remove
        internal void RemoveAll()
        {
            ReadData();
            if (IncludeA) FindFiles(RemoveClip, "Removing animation clips...", "*.anim");
            if (IncludeB) FindFiles(RemoveModel, "Removing FBX files...", "*.fbx");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Process Finished!", "Removed " + EditedCount + " animation events in " + TotalCount + " clips and models!", "OK");
        }

        internal void RemoveClip(string assetPath)
        {                        
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (!clip || clip.events.Length == 0) return;
            var toBeRemained = clip.events.Where(animationEvent => !IsSoundAnimationEvent(animationEvent)).ToArray();
            TotalCount++;
            if (toBeRemained.Length == clip.events.Length) return;
            clip.events = toBeRemained;
            EditedCount++;
            EditorUtility.SetDirty(clip);
        }

        internal void RemoveModel(string assetPath)
        {                        
            var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null) return;
			
            var modified = false;
            var newClips = new List<ModelImporterClipAnimation>();
            TotalCount++;
            foreach (var clip in modelImporter.clipAnimations)
            {
                if (clip.events.Length == 0) continue;
                var toBeRemained = clip.events.Where(animationEvent => !IsSoundAnimationEvent(animationEvent)).ToArray();
                if (toBeRemained.Length == clip.events.Length) continue;
                clip.events = toBeRemained;
                modified = true;
                newClips.Add(clip);
            }

            if (modified)
            {
                modelImporter.clipAnimations = newClips.ToArray();
                AssetDatabase.ImportAsset(assetPath);
                EditedCount++;
            }
        }

        private void RemoveClipXml(ComponentComparisonData data)
        {
            ReadData();
            var xAsset = FindAssetNode(data.AssetPath, false);
            if (XNode.DeepEquals(xAsset, data.ComponentData))
                xAsset.Remove();
            else
            {
                var xComponent = xAsset.Elements().FirstOrDefault(x => XNode.DeepEquals(x, data.ComponentData));
                if (xComponent != null)
                    AsScriptingHelper.RemoveComponentXml(xComponent);
            }
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        #endregion

        private class AsAnimationEventCompare : AsCompareWindow
        {               
            internal static void ShowWindow()
            {
                var window = GetWindow<AsAnimationEventCompare>();
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare Animation Events");
            }

            protected override void DisplayData(ComponentComparisonData data)
            {
                var line = string.Format("{0} ({1})", Path.GetFileNameWithoutExtension(data.AssetPath), data.BackupStatus);
                var clipName = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Name");
                if (!string.IsNullOrEmpty(clipName))
                    line = clipName + ": " + line;
                if (GUILayout.Button(line, GUI.skin.label))
                    AsXmlInfo.Init(data.ComponentData);
            }
            
            protected override void DrawComponentList(List<ComponentComparisonData> dataList, string description, ref Vector2 scrollPosition)
            {
                if (dataList.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(description + " Clips", EditorStyles.boldLabel);
                    if (GUILayout.Button("Save All", GUILayout.Width(110))) SaveAll(dataList);
                    if (GUILayout.Button("Revert All", GUILayout.Width(110))) RevertAll(dataList);
                    if (GUILayout.Button("Remove All", GUILayout.Width(110))) RemoveAll(dataList);
                    EditorGUILayout.EndHorizontal();
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                        foreach (var data in dataList)
                        {
                            EditorGUILayout.BeginHorizontal();
                            DisplayData(data);
                            if (data.BackupStatus != ComponentBackupStatus.Removed)
                            {
                                GUI.contentColor = Color.yellow;
                                if (GUILayout.Button("Locate", EditorStyles.toolbarButton, GUILayout.Width(50)))
                                    LocateComponent(data);
							
                                GUI.contentColor = Color.green;
                                if (dataList != MissingComponents && data.BackupStatus != ComponentBackupStatus.Saved &&
                                    GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                                {
                                    SaveComponent(data);
                                    data.BackupStatus = ComponentBackupStatus.Saved;
                                    AssetDatabase.SaveAssets();
                                }

                                GUI.contentColor = Color.magenta;
                                if (data.BackupStatus != ComponentBackupStatus.Reverted &&
                                    GUILayout.Button("Revert", EditorStyles.toolbarButton, GUILayout.Width(50)))
                                {
                                    RevertComponent(data);
                                    data.BackupStatus = ComponentBackupStatus.Reverted;
                                    AssetDatabase.SaveAssets();
                                }

                                GUI.contentColor = Color.red;
                                if (GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(50)))
                                {
                                    RemoveComponent(data);
                                    data.BackupStatus = ComponentBackupStatus.Removed;
                                    AssetDatabase.SaveAssets();
                                }
                                GUI.contentColor = Color.white;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
                else
                    EditorGUILayout.LabelField("No " + description + " Components Found", EditorStyles.boldLabel);
                EditorGUILayout.Separator();
            }
        
            protected override void LocateComponent(ComponentComparisonData data)
            {
#if UNITY_2018_1_OR_NEWER
                EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
#else
                EditorApplication.ExecuteMenuItem("Window/Animation");
#endif
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(data.AssetPath);
                if (clip) Selection.activeObject = clip;
            }

            private static void SaveComponent(ComponentComparisonData data)
            {
                Instance.UpdateXmlFromClip(data);
            }

            private static void RevertComponent(ComponentComparisonData data)
            {
                Instance.RevertClipToXml(data);
            }

            protected override void RemoveComponent(ComponentComparisonData data)
            {
                Instance.RemoveClipXml(data);
            }
            
            private static void SaveAll(IEnumerable<ComponentComparisonData> dataList)
            {
                foreach (var data in dataList)
                {
                    SaveComponent(data);
                    data.BackupStatus = ComponentBackupStatus.Saved;
                }
                AssetDatabase.SaveAssets();
            }

            private static void RevertAll(IEnumerable<ComponentComparisonData> dataList)
            {
                foreach (var data in dataList)
                {
                    RevertComponent(data);
                    data.BackupStatus = ComponentBackupStatus.Reverted;
                }
                AssetDatabase.SaveAssets();
            }
        }
    }  	
}