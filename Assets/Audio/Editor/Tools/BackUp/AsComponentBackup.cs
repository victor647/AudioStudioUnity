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
    public partial class AsComponentBackup : AsSearchers
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
        private static readonly Dictionary<Type, ComponentExporter> _exporters = new Dictionary<Type, ComponentExporter>();
        private static readonly Dictionary<Type, ComponentImporter> _importers = new Dictionary<Type, ComponentImporter>();
        private readonly Dictionary<Type, ComponentDataBase> _outputTypes = new Dictionary<Type, ComponentDataBase>();
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
        private void RegisterComponent<T>(ComponentImporter importer = null, ComponentExporter exporter = null)
        {
            var t = typeof(T);            
            ComponentsInSearch[t] = true;   
            _outputTypes[t] = new ComponentDataBase();
            _importers[t] = importer;
            _exporters[t] = exporter;
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
                        AudioUtility.CheckoutLockedFile(IndividualXmlPath(component.Key));                                                                    
                }
            }
            else
            {                
                AudioUtility.CheckoutLockedFile(AudioUtility.CombinePath(XmlDocPath, "AudioStudioComponents.xml"));
                fileName = EditorUtility.SaveFilePanel("Export to", XmlDocPath, "AudioStudioComponents.xml", "xml");
                if (string.IsNullOrEmpty(fileName)) return;    
                
            }
            
            if (IncludeA)
                completed |= FindFiles(ParsePrefab, "Exporting Prefabs", "*.prefab");
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
            AudioUtility.CheckoutLockedFile(xmlPath);
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
            if (prefab) 
                ParsePrefabGameObject(prefab, prefabPath);            
        }
        
        private void ParseScene(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
            var scene = SceneManager.GetActiveScene();
            GameObject[] gameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in gameObjects)
            {                
                ParseSceneGameObject(gameObject, scenePath);
            }                
        }        
        
        private void ParsePrefabGameObject(GameObject gameObject, string rootPath)
        {                                    
            var components = gameObject.GetComponentsInChildren<AsComponent>(true);            
            foreach (var component in components)
            {
                var type = component.GetType();
                if (!ComponentsInSearch.ContainsKey(type) || !ComponentsInSearch[type]) continue;
                ParseComponent(component, type, rootPath, SeparateXmlFiles ? _outputTypes[type].XPrefabs : _xPrefabs);
            }
        }  
        
#if UNITY_2018_3_OR_NEWER
        private void ParseSceneGameObject(GameObject gameObject, string rootPath)
        {                                    
            var components = gameObject.GetComponentsInChildren<AsComponent>(true);            
            foreach (var component in components)
            {

                var type = component.GetType();
                var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(component.gameObject);
                if (prefab)
                {
                    var overrides = PrefabUtility.GetObjectOverrides(prefab, true);
                    foreach (var objectOverride in overrides)
                    {
                        var c = objectOverride.instanceObject as AsComponent;
                        if (c == component)
                        {
                            if (ComponentsInSearch.ContainsKey(type) && ComponentsInSearch[type])
                                ParseComponent(component, type, rootPath, SeparateXmlFiles ? _outputTypes[type].XScenes : _xScenes);
                            break;
                        }
                    }
                }
                else
                {
                    if (ComponentsInSearch.ContainsKey(type) && ComponentsInSearch[type])
                        ParseComponent(component, type, rootPath, SeparateXmlFiles ? _outputTypes[type].XScenes : _xScenes);
                }
            }
        } 
#else
        private void ParseSceneGameObject(GameObject gameObject, string rootPath)
        {                                    
            var components = gameObject.GetComponentsInChildren<AsComponent>(true);            
            foreach (var component in components)
            {
                if (PrefabUtility.GetPrefabType(component.gameObject) != PrefabType.None) continue;
                var type = component.GetType();
                if (ComponentsInSearch.ContainsKey(type) && ComponentsInSearch[type]) 
                    ParseComponent(component, type, rootPath, SeparateXmlFiles ? _outputTypes[type].XScenes : _xScenes);
            }
        }  
#endif
        
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
            if (_exporters.ContainsKey(type))
                _exporters[type](component, xComponent);
            return xComponent;
        }
        
        private static string ComputeComponentID(Component component, string type, string path = "", string gameObject = "")
        {
            if (path == "") 
                path = FindComponentPath(component);
            if (gameObject == "") 
                gameObject = GetGameObjectPath(component.transform);
            return AudioUtility.GenerateUniqueID(type + path + gameObject).ToString();		
        }
        #endregion
        
        #region Update
        private static string FindComponentPath(Component component)
        {
            var go = component.gameObject;
            var path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_2018_3_OR_NEWER
                var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                path = stage != null ? stage.prefabAssetPath : SceneManager.GetActiveScene().path;
#else
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
#endif
            }
            return path;
        }        
        
        private void LoadOrCreateXmlDoc(Type type)
        {
            var path = IndividualXmlPath(type);
            if (!File.Exists(path))
            {
                var cdb = new ComponentDataBase();	
                cdb.RefreshElements();
                AudioUtility.WriteXml(path, cdb.XRoot);
            }
            else
            {
                var xRoot = XDocument.Load(path).Root;
                if (xRoot == null) return;
                _outputTypes[type].XRoot = xRoot;
                _outputTypes[type].XPrefabs = xRoot.Element("Prefabs");
                _outputTypes[type].XScenes = xRoot.Element("Scenes");
            }                                                       
        }

        public XElement FindComponentNode(AsComponent component)
        {
            var type = component.GetType();
            var id = ComputeComponentID(component, type.Name);
            LoadOrCreateXmlDoc(type);                        
            var node = _outputTypes[type].XPrefabs.Elements("Component").FirstOrDefault(xComponent => AudioUtility.GetXmlAttribute(xComponent, "ID") == id);
            if (node != null) return node;                   
            node = _outputTypes[type].XScenes.Elements("Component").FirstOrDefault(xComponent => AudioUtility.GetXmlAttribute(xComponent, "ID") == id);
            return node;
        }

        protected internal void RemoveComponentNode(AsComponent component)
        {
            var type = component.GetType();
            var filePath = IndividualXmlPath(type);
            AudioUtility.CheckoutLockedFile(filePath);
            var xComponent = FindComponentNode(component);
            if (xComponent != null)
            {
                xComponent.Remove();
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
            AudioUtility.CheckoutLockedFile(filePath);
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
            AudioUtility.WriteXml(filePath, _outputTypes[type].XRoot);
            return true;
        }
        
#if UNITY_2018_3_OR_NEWER
        public static void SaveComponentAsset(GameObject go)
        {
            var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null) //in prefab edit mode, apply to prefab
                PrefabUtility.SavePrefabAsset(go);
            else //save prefab to scene
            {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(scene, scene.path, false);
            }
        }
#else
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
#endif
        
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
            XRoot = XDocument.Load(filePath).Root;
            
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
            
            XRoot = XDocument.Load(filePath).Root;
            
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
            var xComponents = _xPrefabs.Elements("Component");
            prefabName += ".prefab";            
            foreach (var xComponent in xComponents)
            {
                var xPrefabName = AudioUtility.GetXmlAttribute(xComponent, "Asset");
                if (xPrefabName != prefabName) continue;
                var fullPath = AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(xComponent, "Path"), xPrefabName);                    
                var type = AudioUtility.StringToType(AudioUtility.GetXmlAttribute(xComponent, "Type"));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
                if (prefab == null)
                {
                    Debug.LogError("Import Failed: Can't find prefab at " + fullPath);
                    continue;
                }                
                var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");
                var child = GetGameObject(prefab, objName);
                if (child == null)
                {
                    Debug.LogError("Import Failed: Can't find game object at " + objName + " in prefab " + fullPath);
                    continue;
                }
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
                var xComponents = _xScenes.Elements("Component").ToList();                
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
                    var scene = EditorSceneManager.OpenScene(fullPath);
                    var rootGameObjects = scene.GetRootGameObjects();
                    var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");

                    var sceneEdited = false;
                    foreach (var rootGameObject in rootGameObjects)
                    {
                        var child = GetGameObject(rootGameObject, objName);
                        if (child == null)
                        {
                            Debug.LogError("Import Failed: Can't find game object at " + objName + " in prefab " + fullPath);
                            continue;
                        }
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
                var xComponents = _xPrefabs.Elements("Component").ToList();                
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
                        Debug.LogError("Import Failed: Can't find prefab at " + fullPath);
                        continue;
                    }
                    var objName = AudioUtility.GetXmlAttribute(xComponent, "GameObject");
                    var child = GetGameObject(prefab, objName);
                    if (child == null)
                    {
                        Debug.LogError("Import Failed: Can't find game object at " + objName + " in prefab " + fullPath);
                        continue;
                    }
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
            if(_importers[type] != null && ComponentsInSearch[type])
            {                
                if (_importers[type](component, node)) return true;
            }
            return false;
        }      
        #endregion   
        
        #region Combine
        protected internal void Combine()
        {
            CleanUp();
            AudioUtility.CheckoutLockedFile(DefaultXmlPath);
            foreach (var type in ComponentsInSearch.Keys)
            {                
                LoadOrCreateXmlDoc(type);
            }            
            foreach (var cdb in _outputTypes.Values)
            {
                foreach (var xPrefab in cdb.XPrefabs.Elements("Component"))
                {
                    _xPrefabs.Add(xPrefab);
                    TotalCount++;
                }
                foreach (var xScene in cdb.XScenes.Elements("Component"))
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

            private static bool IsMissing(AsComponent component)
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