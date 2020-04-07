using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Xml.Linq;
using System.Collections.Generic;
using AudioStudio.Components;
using AudioStudio.Editor;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

namespace AudioStudio.Tools
{
	internal class AsComponentLink : AsSearchers
	{				
		[Serializable]
		private class ComponentLink //a data structure that links two components
		{
			internal Type SearchComponent;
			internal Type LinkedComponent;
			internal string ToggleName;
			internal bool WillSearch = true;

			internal ComponentLink(Type search, Type linked)
			{
				SearchComponent = search;
				LinkedComponent = linked;
				ToggleName = search.Name + " (" + linked.Name + ")";				
			}

			internal ComponentLink() {} //empty constructor										
		}

		[Serializable]
		private class CustomTypes //converts string to type
		{
			internal string SearchComponent = "";
			internal string LinkedComponent = "";
			internal ComponentLink CustomLink = new ComponentLink();

			internal void Validate()
			{
				CustomLink.SearchComponent = AsScriptingHelper.StringToType(SearchComponent);
				CustomLink.LinkedComponent = AsScriptingHelper.StringToType(LinkedComponent);				
			}						

			internal bool BothTypesFilled()
			{
				return (CustomLink.LinkedComponent != null && CustomLink.SearchComponent != null);
			}
		}
		
		#region Fields
		private List<ComponentLink> _componentLinks = new List<ComponentLink>();

		private bool _explicitType = true;
		private bool _exportLog = true;
		private bool _removeLinkedAsWell = true;		

		private List<CustomTypes> _customTypeList = new List<CustomTypes>();
		
		private enum ActionType {
			RemoveSearchedComponents, 
			RemoveLinkedComponents,
			RemoveUnlinkedComponents,
			AddUnlinkedComponents, 
			SearchComponents, 
			SearchLinkedComponents,
			SearchUnlinkedComponents,			
		}
		private ActionType _actionType;		
		#endregion
		
		#region Init

		private void OnEnable()
		{						
			_componentLinks.Add(new ComponentLink(typeof(Animator), typeof(AnimationSound)));
			_componentLinks.Add(new ComponentLink(typeof(Animation), typeof(AnimationSound)));
			_componentLinks.Add(new ComponentLink(typeof(PlayableDirector), typeof(TimelineSound)));
			_componentLinks.Add(new ComponentLink(typeof(Button), typeof(ButtonSound)));
			_componentLinks.Add(new ComponentLink(typeof(Dropdown), typeof(DropdownSound)));
			_componentLinks.Add(new ComponentLink(typeof(Slider), typeof(SliderSound)));
			_componentLinks.Add(new ComponentLink(typeof(ScrollRect), typeof(ScrollSound)));
			_componentLinks.Add(new ComponentLink(typeof(Toggle), typeof(ToggleSound)));
			_componentLinks.Add(new ComponentLink(typeof(EventTrigger), typeof(EventSound)));		
			_componentLinks.Add(new ComponentLink(typeof(Camera), typeof(AudioListener3D)));
		}

		private void SwapComponents()
		{
			foreach (var link in _componentLinks)
			{
				var tempType = link.SearchComponent;
				link.SearchComponent = link.LinkedComponent;
				link.LinkedComponent = tempType;
				link.ToggleName = link.SearchComponent.Name + " (" + link.LinkedComponent.Name + ")";
			}
		}
		private void OnDisable()
		{
			_componentLinks.Clear();
		}

		#endregion
		
		#region GUI
		private void OnGUI()
		{
			GUILayout.Label("This tool searches for all the components of a type in the game and apply operations to \nthem or their linked components." +
			                " For example, you can search for all the Buttons and add a \nButtonSound to each Button, or remove all the ButtonSounds from them.");
			using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Quick Select Components");
				if (GUILayout.Button("Swap", GUILayout.Width(60))) SwapComponents();
				if (GUILayout.Button("Select All", GUILayout.Width(100))) SelectAllToggles(true);
				if (GUILayout.Button("Deselect All", GUILayout.Width(100))) SelectAllToggles(false);

				GUILayout.EndHorizontal();
			}
			
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				foreach (var componentData in _componentLinks)
				{
					componentData.WillSearch = GUILayout.Toggle(componentData.WillSearch, componentData.ToggleName);
				}
			}
			
			using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
			{
				IncludeA = GUILayout.Toggle(IncludeA, "Search in prefabs");
				IncludeB = GUILayout.Toggle(IncludeB, "Search in scenes");								
			}

			using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
			{
				_explicitType = GUILayout.Toggle(_explicitType, "Explicit Type");	
				_exportLog = EditorGUILayout.BeginToggleGroup("Export log to xml", _exportLog);					
				EditorGUILayout.EndToggleGroup();
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				GUILayout.Label("Enter custom types, format like 'Button' and 'ButtonSound'");
				CustomTypes toBeRemoved = null;
				foreach (var customLink in _customTypeList)
				{
					GUILayout.BeginHorizontal();
					EditorGUIUtility.labelWidth = 80;
					customLink.SearchComponent = EditorGUILayout.TextField("Type", customLink.SearchComponent);
					customLink.LinkedComponent = EditorGUILayout.TextField("Linked Type", customLink.LinkedComponent);
					if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20))) toBeRemoved = customLink;
					GUILayout.EndHorizontal();
				}
				_customTypeList.Remove(toBeRemoved);
				
				if (GUILayout.Button("+")) _customTypeList.Add(new CustomTypes());													
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				_actionType = (ActionType) EditorGUILayout.EnumPopup("Action", _actionType);
				switch (_actionType)
				{
					case ActionType.SearchComponents:
						GUILayout.Label("Search for a type of component and export locations of all instances");
						_exportLog = true;
						break;
					case ActionType.SearchLinkedComponents:
						GUILayout.Label("Search for occurrences where a type of component and its linked\n" +
						                "type of component are on the same GameObject");
						_exportLog = true;
						break;
					case ActionType.SearchUnlinkedComponents:
						GUILayout.Label("Search for occurrences where a type of component doesn't have its\n" +
						                " linked component on the same GameObject");
						_exportLog = true;
						break;
					case ActionType.RemoveSearchedComponents:
						_removeLinkedAsWell = GUILayout.Toggle(_removeLinkedAsWell, "Remove linked component as well");
						GUILayout.Label("Search for a type of component and remove all instances of the component");
						break;
					case ActionType.RemoveLinkedComponents:
						GUILayout.Label("Search for occurrences where a type of component has its linked\n" +
						                "component on the same GameObject and remove the linked component");
						break;
					case ActionType.RemoveUnlinkedComponents:
						GUILayout.Label("Search for occurrences where a type of component doesn't have its\n" +
						                "linked component on the same GameObject and remove the component itself");
						break;
					case ActionType.AddUnlinkedComponents:
						GUILayout.Label("Search for occurrences where a type of component doesn't have its\n" +
						                "linked component on the same GameObject and add the linked component");
						break;
				}
			}
			
			AsGuiDrawer.DisplaySearchPath(ref SearchPath);
			
			if (GUILayout.Button("Start Searching! (Process can't undo)", EditorStyles.toolbarButton)) 
				StartSearch();		
		}

		private void SelectAllToggles(bool status)
		{
			foreach (var componentLink in _componentLinks)
			{
				componentLink.WillSearch = status;
			}
		}
		#endregion				
		
		#region Validate							
		private bool ValidateComponents()
		{			
			switch (_actionType)
			{
				case ActionType.AddUnlinkedComponents:
				case ActionType.SearchUnlinkedComponents:
				case ActionType.SearchLinkedComponents:
				case ActionType.RemoveLinkedComponents:
				case ActionType.RemoveUnlinkedComponents:
					return TypeCheckBothComponents();					
				case ActionType.SearchComponents:
					return TypeCheckSearchedComponent();
				case ActionType.RemoveSearchedComponents:
					return _removeLinkedAsWell ? TypeCheckBothComponents() : TypeCheckSearchedComponent();
			}
			return false;
		}

		private bool TypeCheckSearchedComponent()
		{
			foreach (var customTypes in _customTypeList)
			{
				customTypes.Validate();
				if (customTypes.CustomLink.SearchComponent != null)
					_componentLinks.Add(customTypes.CustomLink);
				else
				{
					EditorUtility.DisplayDialog("Error", "Invalid or empty type name!", "OK");
					return false;	
				}							
			}
			return true;
		}

		private bool TypeCheckBothComponents()
		{
			foreach (var customTypes in _customTypeList)
			{
				customTypes.Validate();
				if (customTypes.BothTypesFilled())
				{
					_componentLinks.Add(customTypes.CustomLink);							
				}
				else
				{
					EditorUtility.DisplayDialog("Error", "Invalid or empty type name!", "OK");
					return false;	
				}																						
			}
			return true;
		}
		#endregion
		
		#region Search
		private void StartSearch()
		{			
			if (!ValidateComponents()) return;
			
			XRoot.SetAttributeValue("Action", _actionType);			
			if (IncludeA) FindFiles(SearchPrefabs, "Searching Prefabs", "*.prefab");
			if (IncludeB)
			{
				var currentScene = SceneManager.GetActiveScene().path;
				FindFiles(SearchScenes, "Searching Scenes", "*.unity");
				EditorSceneManager.OpenScene(currentScene);
			}
			if (_exportLog) ExportToFile();
			
			CleanUp();
		}
		
		private void SearchPrefabs(string filePath)
		{				
			var prefab = (GameObject) AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject));							
			var xPrefab = new XElement("Prefab");
			xPrefab.SetAttributeValue("AssetPath", filePath);				
			var edited = false;
				
			foreach (var componentData in _componentLinks)
			{
				if (!componentData.WillSearch) continue;
				Component[] components = prefab.GetComponentsInChildren(componentData.SearchComponent, true);
				if (components.Length > 0)
				{						
					if (ProcessComponents(components, componentData, xPrefab, true)) 
						edited = true; 
				}
			}
				
			if (edited)
			{
				if (_actionType != ActionType.SearchComponents && 
				    _actionType != ActionType.SearchLinkedComponents && 
				    _actionType != ActionType.SearchUnlinkedComponents)
					EditorUtility.SetDirty(prefab);
				XRoot.Add(xPrefab);
			}							
		}

		private void SearchScenes(string filePath)
		{
			EditorSceneManager.OpenScene(filePath);
			var scene = SceneManager.GetActiveScene();
			var rootGameObjects = scene.GetRootGameObjects();
				
			var xScene = new XElement("Scene");
			xScene.SetAttributeValue("AssetPath", filePath);
			var edited = false;				
			foreach (var gameObject in rootGameObjects)
			{					
				foreach (var componentLink in _componentLinks)
				{
					if (!componentLink.WillSearch) continue;
					Component[] components = gameObject.GetComponentsInChildren(componentLink.SearchComponent, true);
					if (components.Length > 0)
					{							
						if (ProcessComponents(components, componentLink, xScene, false)) edited = true;
					}
				}
			}

			if (edited)
			{
				if (_actionType != ActionType.SearchComponents && 
				    _actionType != ActionType.SearchLinkedComponents && 
				    _actionType != ActionType.SearchUnlinkedComponents)
					EditorSceneManager.SaveScene(scene, scene.path, false);	
				XRoot.Add(xScene);					
			}
				
			EditorSceneManager.CloseScene(scene, false);
		}

		private bool ProcessComponents(Component[] components, ComponentLink componentLink, XElement xElement, bool isSearchingPrefab)
		{
			var modified = false;
			foreach (var component in components)
			{
				if (_explicitType && component.GetType() != componentLink.SearchComponent) continue;
#if UNITY_2018_3_OR_NEWER
				if (isSearchingPrefab && PrefabUtility.GetPrefabAssetType(component.gameObject) == PrefabAssetType.NotAPrefab) continue;
#else
				if (isSearchingPrefab && PrefabUtility.GetPrefabType(component.gameObject) == PrefabType.None) continue;
#endif
				TotalCount++;
				Component linkedComponent;
				switch (_actionType)
				{
					case ActionType.AddUnlinkedComponents:
						linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);
						if (!linkedComponent)
						{
							component.gameObject.AddComponent(componentLink.LinkedComponent);
							EditedCount++;
							WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
							modified = true;
						}										
						break;
					case ActionType.RemoveSearchedComponents:
						if (componentLink.LinkedComponent != null)
						{
							linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);
							if (linkedComponent) DestroyImmediate(linkedComponent);	
						}						 
						WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
						DestroyImmediate(component, true);	
						modified = true;	
						break;
					case ActionType.RemoveLinkedComponents:
						linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);
						if (linkedComponent)
						{
							EditedCount++;
							WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
							DestroyImmediate(linkedComponent, true);
							modified = true;
						}															
						break;	
					case ActionType.RemoveUnlinkedComponents:
						linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);
						if (!linkedComponent)
						{							
							EditedCount++;
							WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
							DestroyImmediate(component, true);
							modified = true;
						}												
						break;
					case ActionType.SearchComponents:
						WriteXNode(xElement, component, componentLink.SearchComponent);
						break;
					case ActionType.SearchUnlinkedComponents:
						linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);						
						if (!linkedComponent)
						{
							EditedCount++;
							WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
						}
						break;
					case ActionType.SearchLinkedComponents:
						linkedComponent = component.gameObject.GetComponent(componentLink.LinkedComponent);
						if (linkedComponent)
						{
							EditedCount++;
							WriteXNode(xElement, component, componentLink.SearchComponent, componentLink.LinkedComponent);
						}
						break;
				}																																			
			}
			return modified;
		}		
		#endregion
		
		#region XML		
		private void WriteXNode(XElement element, Component component, Type searchType, Type linkedType = null)
		{
			if (!_exportLog) return;
			var xNode = new XElement("Component");
			xNode.SetAttributeValue("Type", searchType.Name);	
			if (linkedType != null) xNode.SetAttributeValue("LinkedType", linkedType.Name);			
			xNode.SetAttributeValue("GameObject", GetGameObjectPath(component.transform));		
			element.Add(xNode);
		}

		private void ExportToFile()
		{
			var fileName = EditorUtility.SaveFilePanel("Export Log", XmlDocDirectory, "Search Result", "xml");
			if (string.IsNullOrEmpty(fileName)) return;
			AsScriptingHelper.WriteXml(fileName, XRoot);
			switch (_actionType)
			{
				case ActionType.SearchComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components!", "OK");
					break;
				case ActionType.RemoveSearchedComponents:
					EditorUtility.DisplayDialog("Success!", "Removed " + TotalCount + " components!", "OK");
					break;
				case ActionType.AddUnlinkedComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components,\n" +
					                                        "Added " + EditedCount + " linked components!", "OK");
					break;
				case ActionType.RemoveLinkedComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components,\n" +
					                                        "Removed " + EditedCount + " linked components!", "OK");
					break;
				case ActionType.RemoveUnlinkedComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components,\n" +
					                                        "Removed " + EditedCount + " unlinked components!", "OK");
					break;
				case ActionType.SearchUnlinkedComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components,\n" +
					                                        "and " + EditedCount + " unlinked components!", "OK");
					break;
				case ActionType.SearchLinkedComponents:
					EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " components,\n" +
					                                        "and " + EditedCount + " linked components!", "OK");
					break;
			}						
		}
		#endregion
	}
}

