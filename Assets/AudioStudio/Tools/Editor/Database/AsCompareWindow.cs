using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
	public enum ComponentBackupStatus
	{
		Unhandled,
		Saved,
		Reverted,
		Removed
	}
	
	public class ComponentComparisonData
	{
		public string AssetPath;
		public ComponentBackupStatus BackupStatus;
		public XElement ComponentData;
	}
	
	public abstract class AsCompareWindow : AsSearchers
	{
		public static readonly List<ComponentComparisonData> MissingComponents = new List<ComponentComparisonData>();
		public static readonly List<ComponentComparisonData> ModifiedComponents = new List<ComponentComparisonData>();
		public static readonly List<ComponentComparisonData> EmptyComponents = new List<ComponentComparisonData>();
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

		protected virtual void DrawComponentList(List<ComponentComparisonData> dataList, string description, ref Vector2 scrollPosition)
		{
			if (dataList.Count > 0)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(description + " Components", EditorStyles.boldLabel);
				if (dataList == MissingComponents && GUILayout.Button("Remove All", GUILayout.Width(110))) RemoveAll(dataList);
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

							GUI.contentColor = Color.red;
							if (dataList == MissingComponents && GUILayout.Button("Remove", EditorStyles.toolbarButton, GUILayout.Width(50)))
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

		protected void RemoveAll(IEnumerable<ComponentComparisonData> dataList)
		{
			foreach (var data in dataList)
			{
				RemoveComponent(data);
				data.BackupStatus = ComponentBackupStatus.Removed;
			}
			AssetDatabase.SaveAssets();
		}

		protected abstract void DisplayData(ComponentComparisonData data);
		protected abstract void LocateComponent(ComponentComparisonData data);
		protected abstract void RemoveComponent(ComponentComparisonData data);
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