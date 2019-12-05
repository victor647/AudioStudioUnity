using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AudioStudio.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AudioStudio.Tools
{
    public class AsAudioStateBackup : AsSearchers
    {						
        private static AsAudioStateBackup _instance;
        public static AsAudioStateBackup Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = CreateInstance<AsAudioStateBackup>();                    
                }
                return _instance;
            }
        }

        protected override string DefaultXmlPath 
        {
            get { return AsScriptingHelper.CombinePath(XmlDocDirectory, "AudioStates.xml"); }
        }

        #region Export
        public void Export()
        {
            CleanUp();
            var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocDirectory, "AudioStates.xml", ".xml");
            if (string.IsNullOrEmpty(fileName)) return;			
            FindFiles(ParseAnimator, "Exporting animation controllers...", "*.controller");			
            AsScriptingHelper.WriteXml(fileName, XRoot);
            EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components in " + EditedCount + " animator controllers!", "OK");		
        }

        public void ParseAnimator(string assetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (!controller) return;	
            var xAsset = new XElement("StateMachine");
            var found = false;
            foreach (var layer in controller.layers)
            {
                foreach (var behaviour in layer.stateMachine.behaviours)
                {
                    var audioState = behaviour as AudioState;
                    if (!audioState) continue;
                    found = true;
                    xAsset.Add(ParseComponent(audioState, layer.name, "OnLayer"));
                }			
                foreach (var state in layer.stateMachine.states)
                {
                    var animationState = state.state;
                    foreach (var behaviour in animationState.behaviours	)
                    {
                        var audioState = behaviour as AudioState;
                        if (!audioState) continue;
                        found = true;
                        xAsset.Add(ParseComponent(audioState, layer.name, animationState.name));
                    }					
                }				
            }
            if (found)
            {
                xAsset.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xAsset);
                EditedCount++;
            }
        }
		
        private XElement ParseComponent(AudioState s, string layer, string stateName)
        {
            var xComponent = new XElement("Component");
            xComponent.SetAttributeValue("Layer", layer);
            xComponent.SetAttributeValue("AnimationState", stateName);
            AsComponentBackup.AudioStateExporter(s, xComponent);
            TotalCount++;
            return xComponent;
        }
        #endregion
		
        #region Import
        public void Import()
        {
            CleanUp();
            var fileName = EditorUtility.OpenFilePanel("Import from", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(fileName) || !ReadData(fileName)) return;
            ImportAnimators(false);
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success!", "Updated " + EditedCount + " animator controllers out of " + TotalCount, "OK");			
        }

        private void ImportAnimators(bool isCompare)
        {
            var xAnimators = XRoot.Elements().ToList();
            TotalCount = xAnimators.Count;
            try
            {
                for (var i = 0; i < TotalCount; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xAnimators[i], "AssetPath");
                    if (!assetPath.Contains(SearchPath)) continue;
                    if (isCompare)
                        CompareAnimator(xAnimators[i]);
                    else
                        ImportAnimator(xAnimators[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying controllers", assetPath, (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            } 
        }

        public bool ImportAnimator(XElement xAnimator)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xAnimator, "AssetPath");
            var modified = false;
            foreach (var xComponent in xAnimator.Elements())
            {
                var component = GetComponentFromXml(assetPath, xComponent, true);
                if (component && AsComponentBackup.AudioStateImporter(component, xComponent))
                {
                    EditorUtility.SetDirty(component);
                    modified = true;
                    EditedCount++;
                }
            }
            return modified;
        }
        #endregion
        
        #region Locate
        private XElement FindAssetNode(string assetPath, bool createIfMissing)
        {
            ReadData();
            var xAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset == null && createIfMissing)
            {
                xAsset = new XElement("StateMachine");
                xAsset.SetAttributeValue("AssetPath", assetPath);
                XRoot.Add(xAsset);
            }
            return xAsset;
        }

        private static XElement FindComponentNode(XElement xAsset, string layer, string state)
        {
            return xAsset.Elements().FirstOrDefault(x => AsScriptingHelper.GetXmlAttribute(x, "Layer") == layer &&
                                                         AsScriptingHelper.GetXmlAttribute(x, "AnimationState") == state);
        }
        
        public bool ComponentBackedUp(string assetPath, string layer, string state)
        {
            var xAsset = FindAssetNode(assetPath, false);
            if (xAsset == null) return false;
            return FindComponentNode(xAsset, layer, state) != null;
        }
        
        public static string GetLayerStateName(StateMachineBehaviour component, ref string stateName)
        {
            var path = AssetDatabase.GetAssetPath(component);
            var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!animator) return string.Empty;
            foreach (var layer in animator.layers)
            {
                if (layer.stateMachine.behaviours.Any(behaviour => behaviour == component))
                {					
                    return layer.name;
                }

                foreach (var state in layer.stateMachine.states)
                {			
                    var animationState = state.state;
                    if (animationState.behaviours.Any(behaviour => behaviour == component))
                    {
                        stateName = animationState.name;
                        return layer.name;
                    }					
                }
            }			
            return string.Empty;
        }

        private static AudioState GetComponentFromXml(string assetPath, XElement xComponent, bool addIfMissing)
        {
            var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (!animator)
            {
                Debug.LogError("Backup Failed: Can't find Animator Controller at " + assetPath);
                return null;
            }
            var xAnimationState = AsScriptingHelper.GetXmlAttribute(xComponent, "AnimationState");
            var xLayer = AsScriptingHelper.GetXmlAttribute(xComponent, "Layer");
            foreach (var layer in animator.layers)
            {
                if (layer.name != xLayer) continue;
                if (xAnimationState == "OnLayer")
                {
                    foreach (var behaviour in layer.stateMachine.behaviours)
                    {
                        var audioState = behaviour as AudioState;
                        if (audioState) return audioState;							
                    }
                    if (addIfMissing) return layer.stateMachine.AddStateMachineBehaviour<AudioState>();
                }
                else
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        var s = state.state;
                        if (s.name != xAnimationState) continue;
                        foreach (var behaviour in s.behaviours)
                        {
                            var audioState = behaviour as AudioState;
                            if (audioState) return audioState;			
                        }
                        if (addIfMissing) return layer.stateMachine.AddStateMachineBehaviour<AudioState>();
                    }	
                }				
            }			
            return null;
        }
        #endregion
        
        #region Compare
        public void Compare()
        {            
            var xmlPath = EditorUtility.OpenFilePanel("Compare with", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(xmlPath) || !ReadData(xmlPath)) return;
            ImportAnimators(true);                                                
            AsAudioStateCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
        
        private void CompareAnimator(XElement xAnimator)
        {
            var assetPath = AsScriptingHelper.GetXmlAttribute(xAnimator, "AssetPath");
            foreach (var xComponent in xAnimator.Elements())
            {
                var data = new ComponentComparisonData
                {
                    AssetPath = assetPath,
                    ComponentData = xComponent,
                    BackupStatus = ComponentBackupStatus.Unhandled
                };
                var component = GetComponentFromXml(assetPath, xComponent, false);
                if (!component)
                    AsCompareWindow.MissingComponents.Add(data);
                else if (!component.IsValid())
                    AsCompareWindow.EmptyComponents.Add(data);
                else
                {
                    var tempComponent = Instantiate(component);
                    if (AsComponentBackup.AudioStateImporter(tempComponent, xComponent))
                        AsCompareWindow.ModifiedComponents.Add(data);
                    DestroyImmediate(tempComponent, true);
                }
            }
        }
        #endregion
		
        #region Update
        public bool UpdateXmlFromComponent(string assetPath, AudioState component)
        {
            var state = "OnLayer";
            var layer = GetLayerStateName(component, ref state);
            var xAsset = FindAssetNode(assetPath, true);
            var xComponent = FindComponentNode(xAsset, layer, state);
            if (xComponent != null)
            {				
                var xTemp = ParseComponent(component, layer, state);
                if (XNode.DeepEquals(xTemp, xComponent)) return false;
                xComponent.ReplaceWith(xTemp);    
            }
            else
                xAsset.Add(ParseComponent(component, layer, state));
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
            return true;
        }
        #endregion
        
        #region Revert
        public bool RevertComponentToXml(string assetPath, AudioState component)
        {            
            var state = "OnLayer";
            var layer = GetLayerStateName(component, ref state);
            var xAsset = FindAssetNode(assetPath, true);
            var xComponent = FindComponentNode(xAsset, layer, state);
            return xComponent != null ? AsComponentBackup.AudioStateImporter(component, xComponent) : UpdateXmlFromComponent(assetPath, component);
        }
        #endregion
        
        #region Remove
        public void RemoveAll()
        {
            CleanUp();
            FindFiles(RemoveAnimator, "Removing Audio States", "*.playable");
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success!", "Removed " + EditedCount + " audio states in " + TotalCount + " animator controllers!", "OK");
        }

        public void RemoveAnimator(string assetPath)
        {                        
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            if (!controller) return;
            var toBeDeleted = new List<AudioState>();
            foreach (var layer in controller.layers)
            {
                toBeDeleted.AddRange(layer.stateMachine.behaviours.OfType<AudioState>());
                foreach (var state in layer.stateMachine.states)
                {
                    toBeDeleted.AddRange(state.state.behaviours.OfType<AudioState>());
                }
            }

            TotalCount++;
            if (toBeDeleted.Count == 0) return;
            foreach (var component in toBeDeleted)
            {
                DestroyImmediate(component);
                EditedCount++;
            }
        }
        
        public void RemoveComponentXml(string assetPath, AudioState component)
        {
            ReadData();
            var state = "OnLayer";
            var layer = GetLayerStateName(component, ref state);
            var xAsset = FindAssetNode(assetPath, false);
            if (xAsset == null) return;
            var xComponent = FindComponentNode(xAsset, layer, state);
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        
        private void RemoveComponentXml(ComponentComparisonData data)
        {
            var xAsset = FindAssetNode(data.AssetPath, false);
            var xComponent = xAsset.Elements().FirstOrDefault(x => XNode.DeepEquals(x, data.ComponentData));
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        #endregion

        private class AsAudioStateCompare : AsCompareWindow
        {               
            public static void ShowWindow()
            {
                var window = GetWindow<AsAudioStateCompare>();
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare AudioStates");
            }

            protected override void DisplayData(ComponentComparisonData data)
            {
                var layer = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Layer");
                var state = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "AnimationState");
                if (GUILayout.Button(string.Format("{0}/{1}: {2} ({3})", layer, state, Path.GetFileNameWithoutExtension(data.AssetPath), data.BackupStatus), GUI.skin.label))
                    AsXmlInfo.Init(data.ComponentData);
            }
        
            protected override void LocateComponent(ComponentComparisonData data)
            {
#if UNITY_2018_1_OR_NEWER
				EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
#else
                EditorApplication.ExecuteMenuItem("Window/Animator");
#endif
                var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(data.AssetPath);
                if (!animator) return;
                foreach (var layer in animator.layers)
                {
                    if (layer.name == AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Layer"))
                    {
                        Selection.activeObject = layer.stateMachine;
                        var state = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "AnimationState");
                        if (state == "OnLayer") return;
                        foreach (var animatorState in layer.stateMachine.states)
                        {
                            if (animatorState.state.name == state)
                            {
                                Selection.activeObject = animatorState.state;
                                return;
                            }
                        }
                    }
                }                              											
            }

            protected override void RemoveComponent(ComponentComparisonData data)
            {
                Instance.RemoveComponentXml(data);
            }      
        }
    }	
}