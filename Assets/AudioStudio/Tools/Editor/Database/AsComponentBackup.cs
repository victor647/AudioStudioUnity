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
using Debug = UnityEngine.Debug;

namespace AudioStudio.Tools
{           
    internal partial class AsComponentBackup : AsSearchers
    {
        #region Fields        
        private delegate bool ComponentImporter(AsComponent component, XElement node);
        private delegate void ComponentExporter(AsComponent component, XElement node);

        internal readonly Dictionary<Type, bool> ComponentsToSearch = new Dictionary<Type, bool>();
        private static readonly Dictionary<Type, ComponentExporter> _exporters = new Dictionary<Type, ComponentExporter>();
        private static readonly Dictionary<Type, ComponentImporter> _importers = new Dictionary<Type, ComponentImporter>();
        private readonly Dictionary<Type, XElement> _outputTypes = new Dictionary<Type, XElement>();
        internal bool SeparateXmlFiles = true;
        internal bool IncludePrefabInScene = true;

        private static AsComponentBackup _instance;
        internal static AsComponentBackup Instance
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
            get { return AsScriptingHelper.CombinePath(XmlDocDirectory, "AudioStudioComponents.xml"); }
        }
        
        private string IndividualXmlPath(Type type)
        {
            return AsScriptingHelper.CombinePath(XmlDocDirectory, type.Name + ".xml");                    
        }
        #endregion       
        
        #region Init                
        private void RegisterComponent<T>(ComponentImporter importer = null, ComponentExporter exporter = null)
        {
            var t = typeof(T);            
            ComponentsToSearch[t] = true;   
            _outputTypes[t] = new XElement("Root");
            _importers[t] = importer;
            _exporters[t] = exporter;
        }

        private void LoadOrCreateXmlDoc(Type type)
        {
            var xmlPath = IndividualXmlPath(type);
            if (!File.Exists(xmlPath))
                AsScriptingHelper.WriteXml(xmlPath, new XElement("Root"));
            else
            {
                var xRoot = XDocument.Load(xmlPath).Root;
                _outputTypes[type] = xRoot;
            }                                                       
        }
        #endregion

        #region Export
        internal void Export()
        {
            var completed = false;
            var fileName = "";
            CleanUp();
            if (SeparateXmlFiles)
            {
                foreach (var pair in ComponentsToSearch)
                {
                    if (pair.Value)
                        _outputTypes[pair.Key] = new XElement("Root");
                }
            }
            else
            {
                fileName = EditorUtility.SaveFilePanel("Export to", XmlDocDirectory, "AudioStudioComponents.xml", "xml");
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
                    foreach (var pair in ComponentsToSearch)
                    {
                        if (pair.Value)
                            AsScriptingHelper.WriteXml(IndividualXmlPath(pair.Key), _outputTypes[pair.Key]);
                    }
                }
                else
                    AsScriptingHelper.WriteXml(fileName, XRoot);
                EditorUtility.DisplayDialog("Process Finished!", "Found " + TotalCount + " components!", "OK");
            }            
        }

        internal void ParsePrefab(string assetPath)
        {            
            var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
            if (!prefab) return;

            if (SeparateXmlFiles)
            {
                foreach (var pair in ComponentsToSearch)
                {
                    if (!pair.Value) continue;
                    var xPrefab = new XElement("Prefab");
                    var components = prefab.GetComponentsInChildren(pair.Key, true);
                    foreach (var component in components)
                    {
                        xPrefab.Add(ParseComponent((AsComponent)component));
                    }
                    if (xPrefab.HasElements)
                    {
                        xPrefab.SetAttributeValue("AssetPath", assetPath);
                        _outputTypes[pair.Key].Add(xPrefab);
                    }
                }
            }
            else
            {
                var xPrefab = new XElement("Prefab");
                var components = prefab.GetComponentsInChildren<AsComponent>(true);
                foreach (var component in components)
                {
                    var type = component.GetType();
                    if (ComponentsToSearch.ContainsKey(type) && ComponentsToSearch[type])
                        xPrefab.Add(ParseComponent(component));
                }
                if (xPrefab.HasElements)
                {
                    xPrefab.SetAttributeValue("AssetPath", assetPath);
                    XRoot.Add(xPrefab);
                }
            }
        }

        internal void ParseScene(string assetPath)
        {
            var scene = EditorSceneManager.OpenScene(assetPath);
            if (!scene.IsValid()) return;
            if (SeparateXmlFiles)
            {
                foreach (var pair in ComponentsToSearch)
                {
                    var xScene = new XElement("Scene");
                    if (!pair.Value) continue;
                    foreach (var rootGameObject in scene.GetRootGameObjects())
                    {
                        var components = rootGameObject.GetComponentsInChildren(pair.Key, true);
                        foreach (var component in components)
                        {
                            if (ComponentBelongsToScene(component))
                                xScene.Add(ParseComponent((AsComponent) component));
                        }
                    }
                    if (xScene.HasElements)
                    {
                        xScene.SetAttributeValue("AssetPath", assetPath);
                        _outputTypes[pair.Key].Add(xScene);
                    }
                }
            }
            else
            {
                var xScene = new XElement("Scene");
                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    var components = rootGameObject.GetComponentsInChildren<AsComponent>(true);
                    foreach (var component in components)
                    {
                        if (ComponentBelongsToScene(component))
                            xScene.Add(ParseComponent(component));
                    }
                }
                if (xScene.HasElements)
                {
                    xScene.SetAttributeValue("AssetPath", assetPath);
                    XRoot.Add(xScene);
                }
            }
        }

        private bool ComponentBelongsToScene(Component component)
        {
#if UNITY_2018_3_OR_NEWER
            var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(component.gameObject);
            if (prefab)
            {
                var overrides = PrefabUtility.GetObjectOverrides(prefab, true);
                foreach (var objectOverride in overrides)
                {
                    var c = objectOverride.instanceObject as AsComponent;
                    if (c == component)
                        return true;
                }
                return false;
            }
#else
            if (!IncludePrefabInScene && PrefabUtility.GetPrefabType(component.gameObject) != PrefabType.None) return false;
#endif
            return true;
        }

        private XElement ParseComponent(AsComponent component)
        {
            var type = component.GetType();
            var xComponent = new XElement("Component");
            xComponent.SetAttributeValue("Type", type.Name);
            xComponent.SetAttributeValue("GameObject", GetGameObjectPath(component.transform));
            _exporters[type](component, xComponent);
            TotalCount++;
            return xComponent;
        }
        #endregion
        
        #region Import
        internal void Import()
        {            
            CleanUp();
            AsCompareWindow.MissingComponents.Clear();
            AsCompareWindow.ModifiedComponents.Clear();
            
            var filePath = EditorUtility.OpenFilePanel("Import from", XmlDocDirectory, "xml");
            if (filePath == null) return;   
            
            XRoot = XDocument.Load(filePath).Root;
            
            if (IncludeA)
                ImportPrefabs(false);
            
            if (IncludeB)
                ImportScenes(false);
                        
            EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Process Finished!", "Updated " + EditedCount + " components out of " + TotalCount, "OK");            
        }

        internal void AutoImport(string searchPath, Type type = null)
        {
            CleanUp();
            if (type != null)
            {
                ComponentsToSearch.Clear();
                ComponentsToSearch[type] = true;
            }
            SearchPath = searchPath;
            LoadOrCreateXmlDoc();       
            ImportPrefabs(false);
            ImportScenes(false);
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        private void ImportScenes(bool isCompare)
        {
            var currentScene = SceneManager.GetActiveScene().path;
            var xScenes = XRoot.Elements("Scene").ToList();                
            var totalCount = xScenes.Count;
            TotalCount += totalCount;
            var searchPath = AsScriptingHelper.ShortPath(SearchPath);
            try
            {
                for (var i = 0; i < xScenes.Count; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xScenes[i], "AssetPath");
                    if (!assetPath.Contains(searchPath)) continue;
                    if (isCompare)
                        CompareScene(xScenes[i]);
                    else
                        ImportScene(xScenes[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying scenes", assetPath, (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
            EditorSceneManager.OpenScene(currentScene);
        }

        internal bool ImportScene(XElement xScene)
        {
            var scenePath = AsScriptingHelper.GetXmlAttribute(xScene, "AssetPath");
            var scene = EditorSceneManager.OpenScene(scenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("Backup Failed: Can't find scene at " + scenePath);
                return false;
            }
            
            var modified = false;
            foreach (var xComponent in xScene.Elements())
            {
                var gameObjectPath = AsScriptingHelper.GetXmlAttribute(xComponent, "GameObject");
                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    var gameObject = GetGameObject(rootGameObject, gameObjectPath);
                    if (gameObject == null) continue;
                    var type = AsScriptingHelper.StringToType(AsScriptingHelper.GetXmlAttribute(xComponent, "Type"));
                    var component = gameObject.GetComponent(type) as AsComponent;
                    if (!component) component = gameObject.AddComponent(type) as AsComponent;
                    if (DiffComponent(component, xComponent))
                    {
                        EditedCount++;
                        modified = true;
                        EditorUtility.SetDirty(component);
                    }
                    break;
                }
            }
            if (modified) EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, false);
            return modified;
        }

        private void ImportPrefabs(bool isCompare)
        {
            var xPrefabs = XRoot.Elements("Prefab").ToList();                
            var totalCount = xPrefabs.Count;
            TotalCount += totalCount;
            var searchPath = AsScriptingHelper.ShortPath(SearchPath);
            try
            {
                for (var i = 0; i < xPrefabs.Count; i++)
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xPrefabs[i], "AssetPath");
                    if (!assetPath.Contains(searchPath)) continue;
                    if (isCompare)
                        ComparePrefab(xPrefabs[i]);
                    else
                        ImportPrefab(xPrefabs[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Modifying prefabs", assetPath, (i + 1f) / TotalCount)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }

        internal bool ImportPrefab(XElement xPrefab)
        {
            var prefabPath = AsScriptingHelper.GetXmlAttribute(xPrefab, "AssetPath");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError("Backup Failed: Can't find prefab at " + prefabPath);
                return false;
            }

            var modified = false;
            foreach (var xComponent in xPrefab.Elements())
            {
                var gameObjectPath = AsScriptingHelper.GetXmlAttribute(xComponent, "GameObject");
                var gameObject = GetGameObject(prefab, gameObjectPath);
                if (gameObject == null)
                {
                    Debug.LogError("Backup Failed: Can't find game object at " + gameObjectPath + " in prefab " + prefabPath);
                    continue;
                }
                var type = AsScriptingHelper.StringToType(AsScriptingHelper.GetXmlAttribute(xComponent, "Type"));
                var component = gameObject.GetComponent(type) as AsComponent;
                if (!component) component = gameObject.AddComponent(type) as AsComponent;
                if (DiffComponent(component, xComponent))
                {
                    EditedCount++;
                    modified = true;
#if UNITY_2018_3_OR_NEWER                    
                    PrefabUtility.SavePrefabAsset(prefab);
#else
                    EditorUtility.SetDirty(prefab);
#endif                    
                }
            }
            return modified;
        }
        #endregion
        
        #region Compare
        internal void Compare()
        {            
            var filePath = EditorUtility.OpenFilePanel("Import from", XmlDocDirectory, "xml");
            if (string.IsNullOrEmpty(filePath)) return;               
            XRoot = XDocument.Load(filePath).Root;
            
            if (IncludeA) ImportPrefabs(true);
            if (IncludeB) ImportScenes(true);    
            AsComponentCompare.ShowWindow();          
            EditorUtility.ClearProgressBar();            
            CleanUp();
        }
        
        private void CompareScene(XElement xScene)
        {
            var scenePath = AsScriptingHelper.GetXmlAttribute(xScene, "AssetPath");
            var scene = EditorSceneManager.OpenScene(scenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("Backup Failed: Can't find scene at " + scenePath);
                return;
            }
            
            foreach (var xComponent in xScene.Elements())
            {
                var gameObjectPath = AsScriptingHelper.GetXmlAttribute(xComponent, "GameObject");
                var type = AsScriptingHelper.StringToType(AsScriptingHelper.GetXmlAttribute(xComponent, "Type"));
                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    var gameObject = GetGameObject(rootGameObject, gameObjectPath);
                    if (gameObject == null) continue;
                    var data = new ComponentComparisonData
                    {
                        AssetPath = scenePath,
                        ComponentData = xComponent,
                        BackupStatus = ComponentBackupStatus.Unhandled
                    };
                    var component = gameObject.GetComponent(type) as AsComponent;
                    if (!component)
                        AsCompareWindow.MissingComponents.Add(data);
                    else if (!component.IsValid())
                        AsCompareWindow.EmptyComponents.Add(data);
                    else
                    {
                        var tempGo = Instantiate(component);
                        if (DiffComponent(tempGo, xComponent))
                            AsCompareWindow.ModifiedComponents.Add(data);
                        DestroyImmediate(tempGo.gameObject, true);
                    }
                    break;
                }
                EditorSceneManager.CloseScene(scene, false);
            }
        }
        
        private void ComparePrefab(XElement xPrefab)
        {
            var prefabPath = AsScriptingHelper.GetXmlAttribute(xPrefab, "AssetPath");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab)
            {
                Debug.LogError("Backup Failed: Can't find prefab at " + prefabPath);
                return;
            }
            
            foreach (var xComponent in xPrefab.Elements())
            {
                var gameObjectPath = AsScriptingHelper.GetXmlAttribute(xComponent, "GameObject");
                var gameObject = GetGameObject(prefab, gameObjectPath);
                if (gameObject == null)
                {
                    Debug.LogError("Backup Failed: Can't find game object at " + gameObjectPath + " in prefab " + prefabPath);
                    continue;
                }
                var type = AsScriptingHelper.StringToType(AsScriptingHelper.GetXmlAttribute(xComponent, "Type"));
                var data = new ComponentComparisonData
                {
                    AssetPath = prefabPath,
                    ComponentData = xComponent,
                    BackupStatus = ComponentBackupStatus.Unhandled
                };
                var component = gameObject.GetComponent(type) as AsComponent;
                if (!component) component = gameObject.AddComponent(type) as AsComponent;
                if (!component)
                    AsCompareWindow.MissingComponents.Add(data);
                else if (!component.IsValid())
                    AsCompareWindow.EmptyComponents.Add(data);
                else
                {
                    var tempGo = Instantiate(component);
                    if (DiffComponent(tempGo, xComponent))
                        AsCompareWindow.ModifiedComponents.Add(data);
                    DestroyImmediate(tempGo.gameObject, true);
                }
            }
        }
        
        private static bool DiffComponent(AsComponent component, XElement node)
        {
            var type = component.GetType();
            return _importers[type] != null && _importers[type](component, node);
        }
        #endregion
        
        #region Locate
        internal static string FindComponentAssetPath(Component component, bool defaultOnPrefab = false)
        {
            var path = AssetDatabase.GetAssetPath(component);
            // path won't be empty if editing on top level of a prefab
            if (string.IsNullOrEmpty(path))
            {
#if UNITY_2018_3_OR_NEWER //2018.3 and later moves prefab editing to a separate stage
                var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                path = stage != null ? stage.assetPath : SceneManager.GetActiveScene().path;
#else
                // if the component is from part of a prefab
                if (PrefabUtility.GetPrefabType(component.gameObject) != PrefabType.None)
                {   
#if UNITY_2018_1_OR_NEWER
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(component.gameObject);
#else
                    var prefab = PrefabUtility.GetPrefabParent(component.gameObject);
#endif
                    if (defaultOnPrefab || EditorUtility.DisplayDialog("Options", "Do you want to apply to prefab or its scene variant?", "Prefab", "Scene"))
                        path = AssetDatabase.GetAssetPath(prefab);
                    else
                        path = SceneManager.GetActiveScene().path;
                } 
                else                
                    path = SceneManager.GetActiveScene().path;
#endif
            }
            return path;
        }

        private XElement FindAssetNode(string assetPath, Type type, bool createIfMissing)
        {
            LoadOrCreateXmlDoc(type);
            var xAsset = _outputTypes[type].Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset == null && createIfMissing)
            {
                xAsset = new XElement(assetPath.EndsWith(".prefab") ? "Prefab" : "Scene");
                xAsset.SetAttributeValue("AssetPath", assetPath);
                _outputTypes[type].Add(xAsset);
            }
            return xAsset;
        }

        private static XElement FindComponentNode(XElement xAsset, Component component)
        {
            return xAsset.Elements().FirstOrDefault(x => GetGameObjectPath(component.transform) == AsScriptingHelper.GetXmlAttribute(x, "GameObject"));
        }

        internal bool ComponentBackedUp(string assetPath, AsComponent component)
        {
            var xAsset = FindAssetNode(assetPath, component.GetType(), false);
            if (xAsset == null) return false;
            return FindComponentNode(xAsset, component) != null;
        }
        #endregion

        #region Update
        internal bool UpdateXmlFromComponent(string assetPath, AsComponent component)
        {                                    
            var type = component.GetType();
            var xAsset = FindAssetNode(assetPath, type, true);
            var xComponent = FindComponentNode(xAsset, component);
            if (xComponent != null)
            {
                var xTemp = ParseComponent(component);                
                if (XNode.DeepEquals(xTemp, xComponent)) return false;
                xComponent.ReplaceWith(xTemp);     
            }
            else
                xAsset.Add(ParseComponent(component));
            AsScriptingHelper.WriteXml(IndividualXmlPath(type), _outputTypes[type]);
            return true;
        }

#if UNITY_2018_3_OR_NEWER
        internal static void SaveComponentAsset(GameObject go, string assetPath)
        {
            if (assetPath.EndsWith(".prefab"))
            {
                var stage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (stage == null)
                    PrefabUtility.SavePrefabAsset(go);
                else
                    EditorUtility.SetDirty(go);
            }
            else
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
#else
        internal static void SaveComponentAsset(GameObject go, string assetPath)
        {
            if (assetPath.EndsWith(".prefab"))
            {
#if UNITY_2018_1_OR_NEWER
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
                var prefab = PrefabUtility.GetPrefabParent(go);
#endif
                var prefabRoot = PrefabUtility.FindPrefabRoot(go);
                PrefabUtility.ReplacePrefab(prefabRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
            }
            else
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
#endif
        #endregion
        
        #region Revert
        internal bool RevertComponentToXml(string assetPath, AsComponent component)
        {
            var xAsset = FindAssetNode(assetPath, component.GetType(), true);
            var xComponent = FindComponentNode(xAsset, component);
            return xComponent != null ? DiffComponent(component, xComponent) : UpdateXmlFromComponent(assetPath, component);
        }
        #endregion
        
        #region Remove
        internal void RemoveAll()
        {
            CleanUp();
            if (IncludeA)
                FindFiles(RemoveAllInPrefab, "Removing Prefabs", "*.prefab");
            if (IncludeB)
            {
                var currentScene = SceneManager.GetActiveScene().path;
                FindFiles(RemoveAllInScene, "Removing Scenes", "*.unity");
                EditorSceneManager.OpenScene(currentScene);
            }
            EditorUtility.DisplayDialog("Process Finished!", "Removed " + TotalCount + " components!", "OK");
        }
        
        internal void RemoveUnsaved()
        {
            CleanUp();
            if (IncludeA)
                FindFiles(RemoveUnsavedInPrefab, "Removing Prefabs not Backed up", "*.prefab");
            if (IncludeB)
            {
                var currentScene = SceneManager.GetActiveScene().path;
                FindFiles(RemoveUnsavedInScene, "Removing Scenes not Backed up", "*.unity");
                EditorSceneManager.OpenScene(currentScene);
            }
            EditorUtility.DisplayDialog("Process Finished!", "Removed " + TotalCount + " components!", "OK");
        }

        internal void RemoveUnsavedInScene(string assetPath)
        {
            var scene = EditorSceneManager.OpenScene(assetPath);
            if (!scene.IsValid()) return;
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                RemoveComponent(assetPath, rootGameObject, false);
            }
            EditorSceneManager.SaveScene(scene);
        }

        internal void RemoveUnsavedInPrefab(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!prefab) return;
            RemoveComponent(assetPath, prefab, false);
#if UNITY_2018_3_OR_NEWER                    
            PrefabUtility.SavePrefabAsset(prefab);
#endif 
        }

        private void RemoveComponent(string assetPath, GameObject gameObject, bool removeAll)
        {
            var components = gameObject.GetComponentsInChildren<AsComponent>(true);
            foreach (var component in components)
            {
                if (removeAll || !ComponentBackedUp(assetPath, component))
                {
                    TotalCount++;
                    DestroyImmediate(component, true);
                }
            }
        }
        
        internal void RemoveAllInScene(string assetPath)
        {                        
            var scene = EditorSceneManager.OpenScene(assetPath);
            if (!scene.IsValid()) return;
            var xAsset = XRoot.Elements("Scene").FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset != null)
                xAsset.Remove();
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                RemoveComponent(assetPath, rootGameObject, true);
            }
            EditorSceneManager.SaveScene(scene);
        }
        
        internal void RemoveAllInPrefab(string assetPath)
        {                        
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!prefab) return;
            var xAsset = XRoot.Elements("Prefab").FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
            if (xAsset != null)
                xAsset.Remove();
            RemoveComponent(assetPath, prefab, true);
#if UNITY_2018_3_OR_NEWER                    
            PrefabUtility.SavePrefabAsset(prefab);
#endif 
        }
        
        // remove node from component inspector
        internal void RemoveComponentXml(string assetPath, AsComponent component)
        {
            var type = component.GetType();
            var xAsset = FindAssetNode(assetPath, type, false);
            if (xAsset == null) return;
            var xComponent = FindComponentNode(xAsset, component);
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(IndividualXmlPath(type), _outputTypes[type]);
        }
        
        // remove node from compare window
        private void RemoveComponentXml(ComponentComparisonData data)
        {
            var type = AsScriptingHelper.StringToType(AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Type"));
            var xAsset = FindAssetNode(data.AssetPath, type, false);
            var xComponent = xAsset.Elements().FirstOrDefault(x => XNode.DeepEquals(x, data.ComponentData));
            if (xComponent == null) return;
            AsScriptingHelper.RemoveComponentXml(xComponent);
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
        }
        #endregion
        
        #region Combine
        internal void Combine()
        {
            ReadData();
            foreach (var type in ComponentsToSearch.Keys)
            {                
                LoadOrCreateXmlDoc(type);
            }            
            foreach (var xType in _outputTypes.Values)
            {
                foreach (var xAsset in xType.Elements())
                {
                    var assetPath = AsScriptingHelper.GetXmlAttribute(xAsset, "AssetPath");
                    var xFullAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
                    if (xFullAsset == null)
                    {
                        xFullAsset = assetPath.EndsWith(".prefab") ? new XElement("Prefab") : new XElement("Scene");
                        xFullAsset.SetAttributeValue("AssetPath", assetPath);
                        XRoot.Add(xFullAsset);
                    }
                    foreach (var xComponent in xAsset.Elements())
                    {
                        xFullAsset.Add(xComponent);
                        TotalCount++;
                    }
                }
            }
            AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
            EditorUtility.DisplayDialog("Success", string.Format("Combined {0} components in prefabs and scenes!", TotalCount), "OK");
        }
        #endregion

        private class AsComponentCompare : AsCompareWindow
        {               
            internal static void ShowWindow()
            {
                var window = (AsComponentCompare) GetWindow(typeof(AsComponentCompare));
                window.position = new Rect(500, 300, 700, 500);     
                window.titleContent = new GUIContent("Compare Components");
            }

            protected override void DisplayData(ComponentComparisonData data)
            {
                var type = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "Type");
                if (GUILayout.Button(string.Format("{0}: {1} ({2})", type, Path.GetFileNameWithoutExtension(data.AssetPath), data.BackupStatus), GUI.skin.label))
                    AsXmlInfo.Init(data.ComponentData);
            }
        
            protected override void LocateComponent(ComponentComparisonData data)
            {
                var go = AsScriptingHelper.GetXmlAttribute(data.ComponentData, "GameObject");
                if (data.AssetPath.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.AssetPath);
                    if (prefab)
                    {                                
                        var child = GetGameObject(prefab, go);
                        if (child) Selection.activeObject = child;
                    }
                }                
                else
                {
                    EditorSceneManager.OpenScene(data.AssetPath);
                    var scene = SceneManager.GetActiveScene();
                    GameObject[] rootGameObjects = scene.GetRootGameObjects();
                    foreach (var rootGameObject in rootGameObjects)
                    {
                        var child = GetGameObject(rootGameObject, go);
                        if (child) Selection.activeObject = child;                        
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