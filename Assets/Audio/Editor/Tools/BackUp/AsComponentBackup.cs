using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Xml.Linq;
using AudioStudio.Components;

namespace AudioStudio.Tools
{           
    public class AsComponentBackup : AsSearchers
    {
        private class ComponentDataBase
        {            
            public XElement XRoot;
            public XElement XPrefabs = new XElement("Prefabs");
            public XElement XScenes = new XElement("Scenes");

            protected internal void RefreshElements()
            {
                XRoot = new XElement("Root");
                XRoot.Add(XPrefabs);
                XRoot.Add(XScenes);
            }
        }
                
        #region Fields        

        private delegate bool ComponentImporter(AsComponent component, XElement node);
        private delegate void ComponentExporter(AsComponent component, XElement node);

        protected internal readonly Dictionary<Type, bool> ComponentsInSearch = new Dictionary<Type, bool>();
        private static readonly Dictionary<Type, ComponentExporter> Exporters = new Dictionary<Type, ComponentExporter>();
        private static readonly Dictionary<Type, ComponentImporter> Importers = new Dictionary<Type, ComponentImporter>();
        private Dictionary<Type, ComponentDataBase> _outputTypes = new Dictionary<Type, ComponentDataBase>();
        protected internal bool SeparateXmlFiles = true;
        
        private XElement _xPrefabs = new XElement("Prefabs");
        private XElement _xScenes = new XElement("Scenes");
        
        private static AsComponentBackup _instance;
        public static AsComponentBackup Instance
        {
            get
            {
                if (!_instance)
                    _instance = CreateInstance<AsComponentBackup>();
                return _instance;
            }
        }

        protected override string DefaultXmlPath 
        {
            get { return AudioUtility.CombinePath(XmlDocPath, "AudioStudioComponents.xml"); }
        }
        
        private string IndividualXmlPath(Type type)
        {
            return AudioUtility.CombinePath(XmlDocPath, type.Name + ".xml");                    
        }
        #endregion       
        
        #region Init                

        private void OnEnable()
        {                                    
            RegisterComponent<AnimationSound>();
            RegisterComponent<AudioTag>(AudioTagImporter, AudioTagExporter);      
            RegisterComponent<ButtonSound>(ButtonSoundImporter, ButtonSoundExporter);
            RegisterComponent<ColliderSound>(ColliderSoundImporter, ColliderSoundExporter);
            RegisterComponent<DropdownSound>(DropdownSoundImporter, DropdownSoundExporter);
            RegisterComponent<EffectSound>(EffectSoundImporter, EffectSoundExporter);
            RegisterComponent<EmitterSound>(EmitterSoundImporter, EmitterSoundExporter);
            RegisterComponent<LoadBank>(LoadBankImporter, LoadBankExporter);
            RegisterComponent<MenuSound>(MenuSoundImporter, MenuSoundExporter);
            RegisterComponent<PeriodSound>(PeriodSoundImporter, PeriodSoundExporter);
            RegisterComponent<ScrollSound>(ScrollSoundImporter, ScrollSoundExporter);            
            RegisterComponent<SliderSound>(SliderSoundImporter, SliderSoundExporter);
            RegisterComponent<ToggleSound>(ToggleSoundImporter, ToggleSoundExporter);
            RegisterComponent<SetSwitch>(SetSwitchImporter, SetSwitchExporter);
        }

        private void RegisterComponent<T>(ComponentImporter importer = null, ComponentExporter exporter = null)
        {
            var t = typeof(T);            
            ComponentsInSearch[t] = true;   
            _outputTypes[t] = new ComponentDataBase();
            Importers[t] = importer;
            Exporters[t] = exporter;
        }    
        
        protected override void CleanUp()
        {            
            _xPrefabs = new XElement("Prefabs");
            _xScenes = new XElement("Scenes");            
            base.CleanUp();
        }
        #endregion

        #region Export

        protected internal void Export()
        {
            var completed = false;
            var fileName = "";
            CleanUp();
            if (SeparateXmlFiles)
            {
                foreach (var component in ComponentsInSearch)
                {
                    if (component.Value)                                            
                        CheckoutLocked(IndividualXmlPath(component.Key));                                                                    
                }
            }
            else
            {                
                CheckoutLocked(AudioUtility.CombinePath(XmlDocPath, "AudioStudioComponents.xml"));
                fileName = EditorUtility.SaveFilePanel("Export to", XmlDocPath, "AudioStudioComponents.xml", "xml");
                if (string.IsNullOrEmpty(fileName)) return;    
                
            }
            
            if (IncludeA)
            {
                completed |= FindFiles(ParsePrefab, "Exporting Prefabs", "*.prefab");                
            }
            if (IncludeB)
            {
                var currentScene = SceneManager.GetActiveScene().path;
                completed |= FindFiles(ParseScene, "Exporting Scenes", "*.unity");
                EditorSceneManager.OpenScene(currentScene);
            }
            
            if (completed)
            {
                if (SeparateXmlFiles)
                {
                    foreach (var component in ComponentsInSearch)
                    {
                        if (component.Value)
                        {
                            _outputTypes[component.Key].RefreshElements();
                            AudioUtility.WriteXml(IndividualXmlPath(component.Key), _outputTypes[component.Key].XRoot);
                        }                                
                    }
                }
                else
                {
                    XRoot.Add(_xPrefabs);
                    XRoot.Add(_xScenes);
                    AudioUtility.WriteXml(fileName, XRoot);
                }                    
                EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components!", "OK");
            }            
        }
        
        public void AutoExport(string searchPath, string xmlPath, Type type = null)
        {
            CleanUp();
            if (type != null)
            {
                ComponentsInSearch.Clear();
                ComponentsInSearch[type] = true;
            }
            SearchPath = searchPath;
            CheckoutLocked(xmlPath);
            FindFiles(ParsePrefab, "Exporting Prefabs", "*.prefab");                           
            FindFiles(ParseScene, "Exporting Scenes", "*.unity");
            
            XRoot.Add(_xPrefabs);
            XRoot.Add(_xScenes);                
            AudioUtility.WriteXml(xmlPath, XRoot);                        
            DestroyImmediate(this);
        }                
        
        private void ParseComponent(AsComponent component, Type type, string rootPath, XElement xParseType)
        {                       
            var xComponent = new XElement("Component");  
            xParseType.Add(WriteComponentToXml(component, type, rootPath, xComponent));
            TotalCount++;
        }
        
        private void ParsePrefab(string prefabPath)
        {            
            var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            if (prefab) ParseGameObject(prefab, prefabPath, true);            
        }
        
        private void ParseScene(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
            var scene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in gameObjects)
            {                
                ParseGameObject(gameObject, scenePath, false);
            }                
        }        
        
        private void ParseGameObject(GameObject gameObject, string rootPath, bool parsingPrefab)
        {                                    
            var components = gameObject.GetComponentsInChildren<AsComponent>(true);            
            foreach (var component in components)
            {
                if (!parsingPrefab && PrefabUtility.GetPrefabType(component.gameObject) != PrefabType.None) continue;
                var type = component.GetType();
                if (!ComponentsInSearch.ContainsKey(type) || !ComponentsInSearch[type]) continue;
                if (SeparateXmlFiles)                
                    ParseComponent(component, type, rootPath, parsingPrefab ? _outputTypes[type].XPrefabs : _outputTypes[type].XScenes);                
                else                
                    ParseComponent(component, type, rootPath, parsingPrefab ? _xPrefabs : _xScenes);                             
            }
        }  
        
        private XElement WriteComponentToXml(AsComponent component, Type type, string rootPath, XElement xComponent)
        {                        
            var gameObject = GetGameObjectPath(component.transform);
            var path = Path.GetDirectoryName(rootPath);
            var fileName = Path.GetFileName(rootPath);
            var id = ComputeComponentID(component, type.Name, rootPath, gameObject);
            xComponent.SetAttributeValue("Type", type.Name);
            xComponent.SetAttributeValue("ID", id);            
            xComponent.SetAttributeValue("Path", path);            
            xComponent.SetAttributeValue("Asset", fileName);
            xComponent.SetAttributeValue("GameObject", gameObject);   
            if (Exporters.ContainsKey(type))
                Exporters[type](component, xComponent);
            return xComponent;
        }
        #endregion
        
        #region Update
        private static string ComputeComponentID(Component component, string type = "", string path = "", string gameObject = "")
        {
            if (type == "") type = component.GetType().Name;
            if (path == "") path = FindComponentPath(component);
            if (gameObject == "") gameObject = GetGameObjectPath(component.transform);
            return AudioUtility.GenerateID(type + path + gameObject).ToString();		
        }

        private static string FindComponentPath(Component component)
        {
            var go = component.gameObject;
            var path = AssetDatabase.GetAssetPath(go);            
            if (string.IsNullOrEmpty(path))
            {
                if (PrefabUtility.GetPrefabType(go) != PrefabType.None)
                {                    
#if UNITY_2018_1_OR_NEWER
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
                    var prefab = PrefabUtility.GetPrefabParent(go);
#endif                                        
                    path = AssetDatabase.GetAssetPath(prefab);
                } 
                else                
                    path = SceneManager.GetActiveScene().path;                					
            }        
            return path;
        }        
        
        private void LoadOrCreateXmlDoc(Type type)
        {
            var path = IndividualXmlPath(type);
            var xRoot = XDocument.Load(path).Element("Root");
            if (xRoot == null)
            {
                var cdb = new ComponentDataBase();			
                AudioUtility.WriteXml(path, cdb.XRoot);
            }
            else
            {
                _outputTypes[type].XRoot = xRoot;
                _outputTypes[type].XPrefabs = xRoot.Element("Prefabs");
                _outputTypes[type].XScenes = xRoot.Element("Scenes");
            }                                                       
        }
        
        private XElement FindComponentNode(AsComponent component)
        {
            var type = component.GetType();
            var id = ComputeComponentID(component);
            LoadOrCreateXmlDoc(type);                        
            var node = _outputTypes[type].XPrefabs.Descendants("Component").FirstOrDefault(xComponent => AudioUtility.GetXmlAttribute(xComponent, "ID") == id);
            if (node != null) return node;                   
            node = _outputTypes[type].XScenes.Descendants("Component").FirstOrDefault(xComponent => AudioUtility.GetXmlAttribute(xComponent, "ID") == id);
            return node;
        }

        protected internal void RemoveComponentNode(AsComponent component)
        {
            var type = component.GetType();
            var filePath = IndividualXmlPath(type);
            CheckoutLocked(filePath);
            var xComponent = FindComponentNode(component);
            if (xComponent != null)
            {
                xComponent.Remove();         
                //_outputTypes[type].RefreshElements();
                AudioUtility.WriteXml(filePath, _outputTypes[type].XRoot);                              
            }            
            DestroyImmediate(component, true);
        }

        public bool RevertComponentToXml(AsComponent component)
        {            
            var xComponent = FindComponentNode(component);
            return xComponent != null ? RefreshComponent(component, xComponent) : UpdateComponentNode(component);
        }

        public bool UpdateComponentNode(AsComponent component)
        {                                    
            var type = component.GetType();
            var filePath = IndividualXmlPath(type);
            CheckoutLocked(filePath);
            var xComponent = FindComponentNode(component);
            if (xComponent != null)
            {
                var path = GetFullAssetPath(xComponent);
                var xTemp = new XElement("Component");
                WriteComponentToXml(component, type, path, xTemp);                
                if (XNode.DeepEquals(xTemp, xComponent)) return false;
                xComponent.ReplaceWith(xTemp);     
            }
            else
            {
                xComponent = new XElement("Component");
                var path = FindComponentPath(component);
                if (path.Contains(".unity"))                                    
                    _outputTypes[type].XScenes.Add(WriteComponentToXml(component, type, path, xComponent));                
                else                
                    _outputTypes[type].XPrefabs.Add(WriteComponentToXml(component, type, path, xComponent));                                                
            }
            //_outputTypes[type].RefreshElements();
            AudioUtility.WriteXml(filePath, _outputTypes[type].XRoot);
            return true;
        }

        public static void SaveComponentAsset(GameObject go)
        {
            EditorUtility.SetDirty(go);
            if (PrefabUtility.GetPrefabType(go) != PrefabType.None)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
                {
#if UNITY_2018_1_OR_NEWER
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
                    var prefab = PrefabUtility.GetPrefabParent(go);
#endif
                    PrefabUtility.ReplacePrefab(GetRootGameObject(go.transform), prefab, ReplacePrefabOptions.ConnectToPrefab);
                }
                else
                    AssetDatabase.SaveAssets();
                return;
            }
            if (EditorUtility.DisplayDialog("Notification", "The component is saved in a scene, do you want to save the scene?", "Yes", "No"))
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene, scene.path, false);			
            }		
        }
        
        private static AsComponent GetComponentFromXml(XElement xComponent, bool addIfNotFound)
        {                                      
            var type = AudioUtility.StringToType(AudioUtility.GetXmlAttribute(xComponent, "Type"));
            var fullPath = GetFullAssetPath(xComponent);
            var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");
            GameObject child = null;
            if (fullPath.EndsWith(".prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
                child = GetGameObject(prefab, objName);                
            }
            else
            {
                var scene = EditorSceneManager.OpenScene(GetFullAssetPath(xComponent));
                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    child = GetGameObject(rootGameObject, objName);
                    if (child) break;                    
                }
            }
            if (!child) return null;
            var component = child.GetComponent(type) as AsComponent;
            if (component) return component;
            if (addIfNotFound) component = child.AddComponent(type) as AsComponent;
            return component;            
        }        
        #endregion
        
        #region Compare

        protected internal void Compare()
        {            
            var filePath = EditorUtility.OpenFilePanel("Import from", XmlDocPath, "xml");
            if (string.IsNullOrEmpty(filePath)) return;               
            XRoot = XDocument.Load(filePath).Element("Root");
            
            if (IncludeA) ImportFromPrefabs(true);
            if (IncludeB) ImportFromScenes(true);    
            AsComponentCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
        #endregion
        
        #region Import

        protected internal void Import()
        {            
            CleanUp();
            AsCompareWindow.MissingComponents.Clear();
            AsCompareWindow.ModifiedComponents.Clear();
            
            var filePath = EditorUtility.OpenFilePanel("Import from", XmlDocPath, "xml");
            if (filePath == null) return;   
            
            XRoot = XDocument.Load(filePath).Element("Root");
            
            if (IncludeA)
                ImportFromPrefabs(false);
            
            if (IncludeB)
                ImportFromScenes(false);
                        
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success!", "Updated " + EditedCount + " components out of " + TotalCount, "OK");            
        }

        public void AutoImport(string searchPath, Type type = null)
        {
            CleanUp();
            if (type != null)
            {
                ComponentsInSearch.Clear();
                ComponentsInSearch[type] = true;
            }
            SearchPath = searchPath;
            LoadOrCreateXmlDoc();       
            ImportFromPrefabs(false);
            ImportFromScenes(false);
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();            
            DestroyImmediate(this);
        }

        protected internal void RevertPrefab(string prefabName)
        {
            CleanUp();                        
            LoadOrCreateXmlDoc();       
            _xPrefabs = XRoot.Element("Prefabs");
            if (_xPrefabs == null) return;
            var xComponents = _xPrefabs.Descendants("Component");
            prefabName += ".prefab";            
            foreach (var xComponent in xComponents)
            {
                var xPrefabName = AudioUtility.GetXmlAttribute(xComponent, "Asset");
                if (xPrefabName != prefabName) continue;
                
                var prefabPath = AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(xComponent, "Path"), xPrefabName);                    
                var type = AudioUtility.StringToType(AudioUtility.GetXmlAttribute(xComponent, "Type"));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning("Import Failed: Can't find prefab at " + prefabPath);
                    continue;
                }                
                var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");
                var child = GetGameObject(prefab, objName);
                if (child == null) continue;
                var component = child.GetComponent(type) as AsComponent;
                if (!component) component = child.AddComponent(type) as AsComponent;
                if (RefreshComponent(component, xComponent))
                    EditorUtility.SetDirty(prefab);                                
            }
            AssetDatabase.SaveAssets();            
            DestroyImmediate(this);
        }
        
        private void ImportFromScenes(bool isCompare)
        {
            try
            {
                var currentScene = SceneManager.GetActiveScene().path;
                _xScenes = XRoot.Element("Scenes");
                if (_xScenes == null || !_xScenes.HasElements) return;
                var xComponents = _xScenes.Descendants("Component").ToList();                
                var totalCount = xComponents.Count;
                TotalCount += totalCount;                
                var searchPath = AudioUtility.ShortPath(SearchPath);
                for (var i = 0; i < xComponents.Count; i++)
                {
                    var xComponent = xComponents[i];
                    var fullPath = GetFullAssetPath(xComponent);
                    if (!fullPath.Contains(searchPath)) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("Reading Scenes", fullPath, i * 1.0f / totalCount)) break;

                    var type = AudioUtility.StringToType(AudioUtility.GetXmlAttribute(xComponent, "Type"));
                    EditorSceneManager.OpenScene(fullPath);
                    var scene = SceneManager.GetActiveScene();
                    GameObject[] rootGameObjects = scene.GetRootGameObjects();
                    var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");

                    var sceneEdited = false;
                    foreach (var rootGameObject in rootGameObjects)
                    {
                        var child = GetGameObject(rootGameObject, objName);
                        if (child == null) continue;
                        var component = child.GetComponent(type) as AsComponent;
                        if (isCompare)
                        {
                            if (!component)
                                AsCompareWindow.MissingComponents.Add(xComponent, "Unhandled");
                            else if (!component.IsValid())
                                AsCompareWindow.EmptyComponents.Add(xComponent, "Unhandled");
                            else
                            {
                                var tempGo = Instantiate(component);
                                if (RefreshComponent(tempGo, xComponent))
                                    AsCompareWindow.ModifiedComponents.Add(xComponent, "Unhandled");
                                DestroyImmediate(tempGo.gameObject, true);
                            }
                        }
                        else
                        {
                            if (!component) component = child.AddComponent(type) as AsComponent;
                            if (RefreshComponent(component, xComponent))
                            {
                                EditedCount++;
                                sceneEdited = true;
                            }
                        }

                        break;
                    }
                    
                    if (sceneEdited) EditorSceneManager.SaveScene(scene, scene.path, false);
                    EditorSceneManager.CloseScene(scene, false);
                }

                EditorSceneManager.OpenScene(currentScene);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }
        
        private void ImportFromPrefabs(bool isCompare)
        {
            try
            {
                _xPrefabs = XRoot.Element("Prefabs");
                if (_xPrefabs == null || !_xPrefabs.HasElements) return;
                var xComponents = _xPrefabs.Descendants("Component").ToList();                
                var totalCount = xComponents.Count;
                TotalCount += totalCount;                
                var searchPath = AudioUtility.ShortPath(SearchPath);
                for (var i = 0; i < xComponents.Count; i++)
                {
                    var xComponent = xComponents[i];
                    var fullPath = GetFullAssetPath(xComponent);
                    if (!fullPath.Contains(searchPath)) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("Reading Prefabs", fullPath, i * 1.0f / totalCount)) break;
                    var type = AudioUtility.StringToType(AudioUtility.GetXmlAttribute(xComponent, "Type"));
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
                    if (prefab == null)
                    {
                        Debug.LogWarning("Import Failed: Can't find prefab at " + fullPath);
                        continue;
                    }

                    var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");
                    var child = GetGameObject(prefab, objName);
                    if (child == null) continue;
                    var component = child.GetComponent(type) as AsComponent;
                    if (isCompare)
                    {
                        if (!component)
                            AsCompareWindow.MissingComponents.Add(xComponent, "Unhandled");
                        else if (!component.IsValid())
                            AsCompareWindow.EmptyComponents.Add(xComponent, "Unhandled");
                        else
                        {
                            var tempGo = Instantiate(component);
                            if (RefreshComponent(tempGo, xComponent))
                                AsCompareWindow.ModifiedComponents.Add(xComponent, "Unhandled");
                            DestroyImmediate(tempGo.gameObject, true);
                        }
                    }
                    else
                    {
                        if (!component) component = child.AddComponent(type) as AsComponent;
                        if (RefreshComponent(component, xComponent))
                        {
                            EditedCount++;
                            EditorUtility.SetDirty(prefab);
                        }
                    }                   
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }        

        private bool RefreshComponent(AsComponent component, XElement node)
        {
            var type = component.GetType();
            if(Importers[type] != null && ComponentsInSearch[type])
            {                
                if (Importers[type](component, node)) return true;
            }
            return false;
        }      
        #endregion   
        
        #region Combine
        protected internal void Combine()
        {
            CleanUp();
            CheckoutLocked(DefaultXmlPath);
            foreach (var type in ComponentsInSearch.Keys)
            {                
                LoadOrCreateXmlDoc(type);
            }            
            foreach (var cdb in _outputTypes.Values)
            {
                foreach (var xPrefab in cdb.XPrefabs.Descendants("Component"))
                {
                    _xPrefabs.Add(xPrefab);
                    TotalCount++;
                }
                foreach (var xScene in cdb.XScenes.Descendants("Component"))
                {
                    _xScenes.Add(xScene);
                    EditedCount++;
                }
            }            
            XRoot.Add(_xPrefabs);
            XRoot.Add(_xScenes);
            AudioUtility.WriteXml(DefaultXmlPath, XRoot);
            EditorUtility.DisplayDialog("Success", string.Format("Combined {0} components in prefabs and {1} in scenes!", TotalCount, EditedCount), "OK");
        }
        #endregion        
        
        #region ComponentExporters               
        private void AudioTagExporter(Component component, XElement node)
        {
            var s = (AudioTag) component; 
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("Tags", s.Tags); 
            node.Add(xSettings);
        }
        
        private void ButtonSoundExporter(Component component, XElement node)
        {
            var s = (ButtonSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.ClickEvents, xEvents, "Click");
            ExportEvent(s.PointerEnterEvent, xEvents, "PointerEnter");
            ExportEvent(s.PointerExitEvent, xEvents, "PointerExit");
            node.Add(xEvents);
        }

        private void ColliderSoundExporter(Component component, XElement node)
        {
            var s = (ColliderSound) component;            
            ExportPhysicsSettings(s, node);            
            ExportParameter(s.CollisionForceParameter, node);
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.EnterEvents, xEvents, "Enter");
            ExportEvents(s.ExitEvents, xEvents, "Exit");
            node.Add(xEvents);                                                          
        }

        private void DropdownSoundExporter(Component component, XElement node)
        {
            var s = (DropdownSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.PopupEvents, xEvents, "Popup");
            ExportEvents(s.ValueChangeEvents, xEvents, "ValueChange");
            ExportEvents(s.CloseEvents, xEvents, "Close");
            node.Add(xEvents);
        }

        private void EffectSoundExporter(Component component, XElement node)
        {
            var s = (EffectSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.EnableEvents, xEvents, "Enable");
            node.Add(xEvents);
        }

        private void EmitterSoundExporter(Component component, XElement node)
        {
            var s = (EmitterSound) component;            
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("FadeInTime", s.FadeInTime);
            xSettings.SetAttributeValue("FadeOutTime", s.FadeOutTime);
            node.Add(xSettings);          
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.AudioEvents, xEvents);                        
            node.Add(xEvents);                                                  
        }
                        
        private void LoadBankExporter(Component component, XElement node)
        {
            var s = (LoadBank) component;    
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("UnloadOnDisable", s.UnloadOnDisable);
            node.Add(xSettings);
            ExportBanks(s.Banks, node);
        }    
        
        private void MenuSoundExporter(Component component, XElement node)
        {
            var s = (MenuSound) component;                   
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.OpenEvents, xEvents, "Open");
            ExportEvents(s.CloseEvents, xEvents, "Close");
            node.Add(xEvents);                                                          
        }

        private void PeriodSoundExporter(Component component, XElement node)
        {
            var s = (PeriodSound) component;                     
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("InitialDelay", s.InitialDelay);
            xSettings.SetAttributeValue("MinInterval", s.MinInterval);
            xSettings.SetAttributeValue("MaxInterval", s.MaxInterval);                                                    
            node.Add(xSettings);            
            ExportEvent(s.AudioEvent, node);
        } 
        
        private void SetSwitchExporter(Component component, XElement node)
        {
            var s = (SetSwitch) component;                    
            ExportPhysicsSettings(s, node);
            var xSettings = new XElement("Settings");      
            xSettings.SetAttributeValue("IsGlobal", s.IsGlobal);
            node.Add(xSettings);     
            var xSwitches = new XElement("Switches");
            ExportSwitches(s.OnSwitches, xSwitches, "On");
            ExportSwitches(s.OffSwitches, xSwitches, "Off");            
            node.Add(xSwitches);            
        }                            
        
        private void ToggleSoundExporter(Component component, XElement node)
        {
            var s = (ToggleSound) component;                      
            var xEvents = new XElement("AudioEvents");
            ExportEvents(s.ToggleOnEvents, xEvents, "ToggleOn");
            ExportEvents(s.ToggleOffEvents, xEvents, "ToggleOff");
            node.Add(xEvents);           
        }

        private void ScrollSoundExporter(Component component, XElement node)
        {
            var s = (ScrollSound) component;                                           
            ExportEvent(s.ScrollEvent, node);            
        }

        private void SliderSoundExporter(Component component, XElement node)
        {
            var s = (SliderSound) component;                
            ExportParameter(s.ConnectedParameter, node);
            ExportEvent(s.DragEvent, node);                                                                                                                               
        }       
        #endregion 
        
        #region ComponentImporters                
        private bool AudioTagImporter(Component component, XElement node)
        {
            var s = (AudioTag) component;    
            var xSettings = node.Element("Settings");
            return ImportEnum(ref s.Tags, AudioUtility.GetXmlAttribute(xSettings, "Tags"));            
        }
        
        private bool ButtonSoundImporter(Component component, XElement node)
        {
            var s = (ButtonSound) component;
            var modified = ImportEvents(ref s.ClickEvents, node, "Click");
            modified |= ImportEvent(ref s.PointerEnterEvent, node, "PointerEnter");
            modified |= ImportEvent(ref s.PointerExitEvent, node, "PointerExit");
            return modified;
        }

        private bool ColliderSoundImporter(Component component, XElement node)
        {
            var s = (ColliderSound) component;
            var modified = ImportPhysicsSettings(s, node);            
            modified |= ImportParameter(ref s.CollisionForceParameter, out s.ValueScale, node);
            modified |= ImportEvents(ref s.EnterEvents, node, "Enter");
            modified |= ImportEvents(ref s.ExitEvents, node, "Exit");
            return modified;
        }
        
        private bool DropdownSoundImporter(Component component, XElement node)
        {            
            var s = (DropdownSound) component;
            var modified = ImportEvents(ref s.ValueChangeEvents, node, "ValueChange");
            modified |= ImportEvents(ref s.PopupEvents, node, "Popup");
            modified |= ImportEvents(ref s.CloseEvents, node, "Close");
            return modified;
        }
    
        private bool EffectSoundImporter(Component component, XElement node)
        {
            var s = (EffectSound) component;
            var modified = ImportEvents(ref s.EnableEvents, node, "Enable");
            return modified;
        }

        private bool EmitterSoundImporter(Component component, XElement node)
        {
            var s = (EmitterSound) component;
            var xSettings = node.Element("Settings");                        
            var modified = ImportEvents(ref s.AudioEvents, node);
            modified |= ImportFloat(ref s.FadeInTime, AudioUtility.GetXmlAttribute(xSettings, "FadeInTime"));
            modified |= ImportFloat(ref s.FadeOutTime, AudioUtility.GetXmlAttribute(xSettings, "FadeOutTime"));            
            return modified;
        }
                        
        private bool LoadBankImporter(Component component, XElement node)
        {
            var s = (LoadBank) component;    
            var xSettings = node.Element("Settings");
            var modified = ImportBool(ref s.UnloadOnDisable, AudioUtility.GetXmlAttribute(xSettings, "UnloadOnDisable"));
            modified |= ImportBanks(ref s.Banks, node);
            return modified;
        }    
        
        private bool MenuSoundImporter(Component component, XElement node)
        {
            var s = (MenuSound) component;                   
            return ImportEvents(ref s.OpenEvents, node, "Open") ||            
                   ImportEvents(ref s.CloseEvents, node, "Close");            
        }

        private bool PeriodSoundImporter(Component component, XElement node)
        {
            var s = (PeriodSound) component;           
            var xSettings = node.Element("Settings");            
            var modified = ImportEvent(ref s.AudioEvent, node.Element("AudioEvent"));
            modified |= ImportFloat(ref s.InitialDelay, AudioUtility.GetXmlAttribute(xSettings, "InitialDelay"));
            modified |= ImportFloat(ref s.MinInterval, AudioUtility.GetXmlAttribute(xSettings, "MinInterval"));
            modified |= ImportFloat(ref s.MaxInterval, AudioUtility.GetXmlAttribute(xSettings, "MaxInterval"));
            return modified;
        }
        
        private bool SetSwitchImporter(Component component, XElement node)
        {
            var s = (SetSwitch) component;                    
            var modified = ImportPhysicsSettings(s, node);
            var xSettings = node.Element("Settings");              
            modified |= ImportBool(ref s.IsGlobal, AudioUtility.GetXmlAttribute(xSettings, "IsGlobal"));
            modified |= ImportSwitches(ref s.OnSwitches, node, "On");
            modified |= ImportSwitches(ref s.OffSwitches, node, "Off");                           
            return modified;           
        }                            
        
        private bool ToggleSoundImporter(Component component, XElement node)
        {
            var s = (ToggleSound) component;
            var modified = ImportEvents(ref s.ToggleOnEvents, node, "ToggleOn");
            modified |= ImportEvents(ref s.ToggleOffEvents, node, "ToggleOff");
            return modified;
        }

        private bool ScrollSoundImporter(Component component, XElement node)
        {
            var s = (ScrollSound) component;                                           
            return ImportEvent(ref s.ScrollEvent, node);            
        }

        private bool SliderSoundImporter(Component component, XElement node)
        {
            var s = (SliderSound) component;
            var modified = ImportParameter(ref s.ConnectedParameter, out s.ValueScale, node);
            modified |= ImportEvent(ref s.DragEvent, node);
            return modified;
        }       
        #endregion

        private class AsComponentCompare : AsCompareWindow
        {               
            public static void ShowWindow()
            {
                var window = (AsComponentCompare) GetWindow(typeof(AsComponentCompare));
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare Components");
            }

            protected override void DisplayData(string fullPath, XElement node, string status)
            {
                if (GUILayout.Button(AudioUtility.GetXmlAttribute(node, "Type") + ": " + fullPath.Substring(7) + " (" + status + ")", GUI.skin.label))
                {
                    AsXmlInfo.Init(node);
                }
            }
        
            protected override void LocateComponent(XElement node)
            {
                var fullPath = GetFullAssetPath(node);
                var go = AudioUtility.GetXmlAttribute(node, "GameObject");
                if (fullPath.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
                    if (prefab)
                    {                                
                        var child = GetGameObject(prefab, go);
                        if (child) Selection.activeObject = child;
                    }
                }                
                else
                {
                    EditorSceneManager.OpenScene(fullPath);
                    var scene = SceneManager.GetActiveScene();
                    GameObject[] rootGameObjects = scene.GetRootGameObjects();
                    foreach (var rootGameObject in rootGameObjects)
                    {
                        var child = GetGameObject(rootGameObject, go);
                        if (child) Selection.activeObject = child;                        
                    }
                }
            }
        
            protected override void SaveComponent(XElement node)
            {                        
                var component = GetComponentFromXml(node, true);
                if (IsMissing(component)) return;
                if (Instance.UpdateComponentNode(component))                
                    SaveComponentAsset(component.gameObject);                                    
            }
        
            protected override void RevertComponent(XElement node)
            {       
                var component = GetComponentFromXml(node, true);
                if (IsMissing(component)) return;
                if (Instance.RevertComponentToXml(component))                                    
                    SaveComponentAsset(component.gameObject);                                    
            }

            protected override void RemoveComponent(XElement node)
            {
                var component = GetComponentFromXml(node, false);
                Instance.RemoveComponentNode(component);
                if (!component) return;
                var go = component.gameObject;                                            
                SaveComponentAsset(go);                
            }

            private bool IsMissing(AsComponent component)
            {
                if (!component)
                {
                    EditorUtility.DisplayDialog("Error", "Component is missing, you might have removed it already!", "OK");
                    return true;
                }
                return false;
            }
        }
    }                
}