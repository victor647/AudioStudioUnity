using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AudioStudio
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
		
        protected override string DefaultXmlPath {
            get
            {
                return AudioUtility.CombinePath(XmlDocPath, "AudioStates.xml");        
            }
        }
		
        private void OnGUI()
        {
            GUILayout.Label("This tool searches for animator controllers (.controller files) and export data to an xml file. \n" +
                            "It can also import data from xml and update audio states into the controllers.");

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
            var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocPath, "AudioStates.xml", ".xml");
            if (string.IsNullOrEmpty(fileName)) return;			
            FindFiles(ParseAnimator, "Exporting animation controllers...", "*.controller");			
            AudioUtility.WriteXml(fileName, XRoot);
            EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components in " + EditedCount + " animator controllers!", "OK");		
        }

        private void ParseAnimator(string filePath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(filePath);
            if (!controller) return;		
            foreach (var layer in controller.layers)
            {
                foreach (var behaviour in layer.stateMachine.behaviours)
                {
                    var s = behaviour as AudioState;
                    if (s)
                    {
                        var xComponent = new XElement("Component");
                        XRoot.Add(WriteComponentToXml(s, filePath, layer.name, "OnLayer", xComponent));
                    }
                }			
                foreach (var state in layer.stateMachine.states)
                {
                    var animationState = state.state;
                    foreach (var behaviour in animationState.behaviours	)
                    {
                        var s = behaviour as AudioState;
                        if (s)
                        {
                            var xComponent = new XElement("Component");
                            XRoot.Add(WriteComponentToXml(s, filePath, layer.name, animationState.name, xComponent));
                        }
                    }					
                }				
            }
            EditedCount++;
        }
		
        private XElement WriteComponentToXml(AudioState s, string filePath, string layer, string stateName, XElement xComponent)
        {						
            var path = Path.GetDirectoryName(filePath);
            var asset = Path.GetFileName(filePath);			
            xComponent.SetAttributeValue("Path", path);
            xComponent.SetAttributeValue("Asset", asset);
            xComponent.SetAttributeValue("Layer", layer);
            xComponent.SetAttributeValue("AnimationState", stateName);
            var xSettings = new XElement("Settings");			
            xSettings.SetAttributeValue("AudioState", s.AnimationAudioState.ToString());						
            xSettings.SetAttributeValue("StopEventOnExit", s.StopEventsOnExit);
            xSettings.SetAttributeValue("ResetStateOnExit", s.ResetStateOnExit);
            xComponent.Add(xSettings);
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.EnterEvents, xEvents, "Enter");
            ExportEvents(s.ExitEvents, xEvents, "Exit");
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
            AsAudioStateCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
        #endregion
		
        #region Update
        private XElement FindComponentNode(string fullPath, string layer, string state)
        {            
            LoadOrCreateXmlDoc();            			
            var xComponents = XRoot.Descendants("Component");
            foreach (var xComponent in xComponents)
            {
                if (AudioUtility.GetXmlAttribute(xComponent, "Layer") == layer &&
                    AudioUtility.GetXmlAttribute(xComponent, "AnimationState") == state &&
                    GetFullAssetPath(xComponent) == fullPath)
                {
                    return xComponent;
                }
            }
            return null;
        }

        public void RevertComponentToXml(string fullPath, string layer, string state, AudioState component)
        {            
            var xComponent = FindComponentNode(fullPath, layer, state);
            if (xComponent != null)
            {				
                if (AudioStateImporter(component, xComponent))
                {
                    EditorUtility.SetDirty(component);
                }                  
            }
            DestroyImmediate(this);
        }

        public void UpdateComponentNode(string fullPath, string layer, string state, AudioState component)
        {
            CheckoutLocked(DefaultXmlPath);												
            var xComponent = FindComponentNode(fullPath, layer, state);
            if (xComponent != null)
            {				
                xComponent.RemoveAll();
                WriteComponentToXml(component, fullPath, layer, state, xComponent);
            }
            else
            {                				
                xComponent = new XElement("Component");				
                XRoot.Add(WriteComponentToXml(component, fullPath, layer, state, xComponent));                
            }
            AudioUtility.WriteXml(DefaultXmlPath, XRoot);
            DestroyImmediate(this);
        }
		
        public void RemoveComponentNode(string fullPath, string layer, string state)
        {
            CheckoutLocked(DefaultXmlPath);
            var xComponent = FindComponentNode(fullPath, layer, state);
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
            EditorUtility.DisplayDialog("Success!", "Updated " + EditedCount + " animator controllers out of " + TotalCount, "OK");			
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
                    if (isCompare)
                    {
                        var component = GetComponentFromXml(xComponent, false);
                        if (!component)
                            AsCompareWindow.MissingComponents.Add(xComponent, "Unhandled");
                        else if (!component.IsValid())
                            AsCompareWindow.EmptyComponents.Add(xComponent, "Unhandled");
                        else
                        {
                            var tempComponent = Instantiate(component);
                            if (AudioStateImporter(tempComponent, xComponent))
                                AsCompareWindow.ModifiedComponents.Add(xComponent, "Unhandled");
                            DestroyImmediate(tempComponent, true);
                        }						
                    }				
                    else
                    {
                        var component = GetComponentFromXml(xComponent, true);						
                        if (AudioStateImporter(component, xComponent))
                        {
                            EditorUtility.SetDirty(component);
                            EditedCount++;
                        }
                    }										
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying controllers", GetFullAssetPath(xComponent), current * 1.0f / TotalCount)) break;
                    current++;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            } 
        }

        public static string GetLayerStateName(StateMachineBehaviour component, ref string stateName)
        {
            var path = AssetDatabase.GetAssetPath(component);
            var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
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

        private static AudioState GetComponentFromXml(XElement xComponent, bool addIfMissing)
        {
            var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(GetFullAssetPath(xComponent));
            if (!animator) return null;
            var xAnimationState = AudioUtility.GetXmlAttribute(xComponent, "AnimationState");
            var xLayer = AudioUtility.GetXmlAttribute(xComponent, "Layer");
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

        private static bool AudioStateImporter(AudioState audioState, XElement xState)
        {
            var xSettings = xState.Element("Settings");
            var modified = ImportEnum(ref audioState.AnimationAudioState, AudioUtility.GetXmlAttribute(xSettings, "AudioState"));						
            modified |= ImportBool(ref audioState.StopEventsOnExit, AudioUtility.GetXmlAttribute(xSettings, "StopEventOnExit"));
            modified |= ImportBool(ref audioState.ResetStateOnExit, AudioUtility.GetXmlAttribute(xSettings, "ResetStateOnExit"));
            modified |= ImportEvents(ref audioState.EnterEvents, xState, "Enter");
            modified |= ImportEvents(ref audioState.ExitEvents, xState, "Exit");
            return modified;
        }
        #endregion

        private class AsAudioStateCompare : AsCompareWindow
        {               
            public static void ShowWindow()
            {
                var window = (AsAudioStateCompare) GetWindow(typeof(AsAudioStateCompare));
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare AudioStates");
            }

            protected override void DisplayData(string fullPath, XElement node, string status)
            {
                var layer = AudioUtility.GetXmlAttribute(node, "Layer");
                var state = AudioUtility.GetXmlAttribute(node, "AnimationState");
                if (GUILayout.Button(layer + "/" + state + ": " + fullPath.Substring(7) + " (" + status + ")", GUI.skin.label))
                {
                    AsXmlInfo.Init(node);
                }
            }
        
            protected override void LocateComponent(XElement node)
            {
#if UNITY_2018_1_OR_NEWER
				EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
#else
                EditorApplication.ExecuteMenuItem("Window/Animator");
#endif
                var animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(GetFullAssetPath(node));
                if (!animator) return;
                foreach (var layer in animator.layers)
                {
                    if (layer.name == AudioUtility.GetXmlAttribute(node, "Layer"))
                    {
                        Selection.activeObject = layer.stateMachine;
                        var state = AudioUtility.GetXmlAttribute(node, "AnimationState");
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
        
            protected override void SaveComponent(XElement node)
            {                        
                var audioState = GetComponentFromXml(node, false);            
                var state = "OnLayer";
                var layer = GetLayerStateName(audioState, ref state);
                Instance.UpdateComponentNode(GetFullAssetPath(node), layer, state, audioState);
            }
		
            protected override void RevertComponent(XElement node)
            {
                var audioState = GetComponentFromXml(node, true);
                if (AudioStateImporter(audioState, node))
                {
                    EditorUtility.SetDirty(audioState);				
                }            									         
            }

            protected override void RemoveComponent(XElement node)
            {			
                var audioState = GetComponentFromXml(node, false);
                if (audioState)
                {
                    EditorUtility.SetDirty(audioState);
                    DestroyImmediate(audioState, true);
                }
                Instance.RemoveComponentNode(GetFullAssetPath(node), AudioUtility.GetXmlAttribute(node, "Layer"), AudioUtility.GetXmlAttribute(node, "AnimationState"));
            }      
        }
    }	
}