using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
	public abstract class AsCompareWindow : AsSearchers
	{
		public static readonly Dictionary<XElement, string> MissingComponents = new Dictionary<XElement, string>();
		public static readonly Dictionary<XElement, string> ModifiedComponents = new Dictionary<XElement, string>();
		public static readonly Dictionary<XElement, string> EmptyComponents = new Dictionary<XElement, string>();
		private Vector2 _scrollPosition1, _scrollPosition2, _scrollPosition3;

		private void OnDestroy()
		{
			MissingComponents.Clear();
			ModifiedComponents.Clear();
			EmptyComponents.Clear();
		}

		private void OnGUI()
		{
			DrawComponentList(MissingComponents, "Missing", ref _scrollPosition1);
			DrawComponentList(ModifiedComponents, "Modified", ref _scrollPosition2);
			DrawComponentList(EmptyComponents, "Empty", ref _scrollPosition3);
		}

		private void DrawComponentList(Dictionary<XElement, string> dataList, string description, ref Vector2 scrollPosition)
		{
			if (dataList.Count > 0)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(description + " Components", EditorStyles.boldLabel);
				if (GUILayout.Button("Save All", GUILayout.Width(110))) SaveAll(dataList);
				if (GUILayout.Button("Revert All", GUILayout.Width(110))) RevertAll(dataList);
				if (GUILayout.Button("Remove All", GUILayout.Width(110))) RemoveAll(dataList);
				EditorGUILayout.EndHorizontal();
				using (new EditorGUILayout.VerticalScope(GUI.skin.box))
				{
					scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
					var tempList = new Dictionary<XElement, string>(dataList);
					foreach (var data in tempList.Keys)
					{
						EditorGUILayout.BeginHorizontal();
						var fullPath = GetFullAssetPath(data);
						DisplayData(fullPath, data, dataList[data]);
						GUI.contentColor = Color.yellow;						
						if (GUILayout.Button("Locate", EditorStyles.toolbarButton, GUILayout.Width(50)))
						{							
							LocateComponent(data);
						}						
						
						GUI.contentColor = Color.green;
						if (dataList != MissingComponents && dataList[data] == "Unhandled" && GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
						{
							SaveComponent(data);
							dataList[data] = "Saved";
						}

						GUI.contentColor = Color.magenta;
						if (dataList[data] != "Reverted" && GUILayout.Button("Revert", EditorStyles.toolbarButton, GUILayout.Width(50)))
						{
							RevertComponent(data);
							dataList[data] = "Reverted";
						}

						GUI.contentColor = Color.red;
						if (dataList[data] != "Removed" && GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(50)))
						{
							RemoveComponent(data);
							dataList[data] = "Removed";
						}

						GUI.contentColor = Color.white;
						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.EndScrollView();
				}
			}
			else
			{
				EditorGUILayout.LabelField("No " + description + " Components Found", EditorStyles.boldLabel);
			}

			EditorGUILayout.Separator();
		}

		private void SaveAll(Dictionary<XElement, string> dataList)
		{
			var tempList = new Dictionary<XElement, string>(dataList);
			foreach (var data in tempList.Keys)
			{
				SaveComponent(data);
				dataList[data] = "Saved";
			}

			AssetDatabase.SaveAssets();
		}

		private void RevertAll(Dictionary<XElement, string> dataList)
		{
			var tempList = new Dictionary<XElement, string>(dataList);
			foreach (var data in tempList.Keys)
			{
				RevertComponent(data);
				dataList[data] = "Reverted";
			}

			AssetDatabase.SaveAssets();
		}

		private void RemoveAll(Dictionary<XElement, string> dataList)
		{
			var tempList = new Dictionary<XElement, string>(dataList);
			foreach (var data in tempList.Keys)
			{
				RemoveComponent(data);
				dataList[data] = "Removed";
			}

			AssetDatabase.SaveAssets();
		}

		protected abstract void DisplayData(string fullPath, XElement node, string status);
		protected abstract void LocateComponent(XElement node);
		protected abstract void SaveComponent(XElement node);
		protected abstract void RevertComponent(XElement node);
		protected abstract void RemoveComponent(XElement node);
	}

	public class AsXmlInfo : EditorWindow
	{
		private XElement _xComponent;
		private static AsXmlInfo _instance;
		private int _lines;
		private int _maxChar = 20;
		public static void Init(XElement node)
		{					
			var position = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			position.y += 10;
			_instance = CreateInstance<AsXmlInfo>();
			_instance.ShowAsDropDown(new Rect(position, Vector2.zero), Vector2.zero);			
			_instance.titleContent = new GUIContent("Original");
			_instance._xComponent = node;
		}
		
		private void OnGUI()
		{
			_instance.minSize = new Vector2(_maxChar * 7, _lines * 22);
			_lines = _maxChar = 0;			
			foreach (var child in _xComponent.Elements())
			{								
				DisplayXml(child);										
			}					
		}

		private void DisplayXml(XElement node)
		{
			if (node.HasAttributes)
			{
				EditorGUILayout.LabelField(node.Name + ": ", EditorStyles.boldLabel);
				_lines++;
				using (new GUILayout.VerticalScope("box"))
				{
					foreach (var attribute in node.Attributes())
					{
						var label = "  " + attribute.Name + ": " + attribute.Value;
						EditorGUILayout.LabelField(label);
						_maxChar = Mathf.Max(_maxChar, label.Length);
						_lines++;
					}
				}
			}

			foreach (var child in node.Elements())
			{								
				DisplayXml(child);				
			}	
		}
	}
}