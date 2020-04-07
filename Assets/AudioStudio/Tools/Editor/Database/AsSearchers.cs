using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AudioStudio.Tools
{
	public enum XmlAction
	{
		Save,
		Revert,
		Remove
	}
	
	internal abstract class AsSearchers : EditorWindow
	{		
		internal bool IncludeA = true;
		internal bool IncludeB = true;
		protected static string SearchPath = "Assets";

		protected string XmlDocDirectory
		{
			get { return AudioPathSettings.EditorConfigPathFull; }
		}
		protected int EditedCount;
		protected int TotalCount;
		protected XElement XRoot = new XElement("Root");

		protected virtual string DefaultXmlPath
		{
			get { return ""; }
		}

		internal void OpenXmlFile()
		{
			if (File.Exists(DefaultXmlPath))
				Process.Start(DefaultXmlPath);
			else
				EditorUtility.DisplayDialog("Error", "Default xml file does not exist!", "OK");
		}

		//reset fields to prevent duplicate
		protected void CleanUp()
		{
			XRoot = new XElement("Root");
			TotalCount = 0;
			EditedCount = 0;
		}

		protected void LoadOrCreateXmlDoc()
		{
			try
			{
				XRoot = XDocument.Load(DefaultXmlPath).Root;
			}
#pragma warning disable 168
			catch (FileNotFoundException e)
#pragma warning restore 168
			{
				XRoot = new XElement("Root");				
				AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
			}
		}
		
		protected bool ReadData(string fileName = "")
		{
			CleanUp();
			if (string.IsNullOrEmpty(fileName))
				LoadOrCreateXmlDoc();
			else
			{
				XRoot = XDocument.Load(fileName).Root;
				if (XRoot == null)
				{
					EditorUtility.DisplayDialog("Error", "Xml format is invalid!", "OK");
					return false;
				}
			}
			return true;
		}

		protected static bool FindFiles(Action<string> parser, string progressBarTitle, string extension, string searchFolder = "")
		{
			try
			{
				if (searchFolder == "")
					searchFolder = SearchPath;
				EditorUtility.DisplayCancelableProgressBar(progressBarTitle, "Loading assets...", 0);
				string[] allFiles = Directory.GetFiles(searchFolder, extension, SearchOption.AllDirectories);
				for (var i = 0; i < allFiles.Length; i++)
				{
					var shortPath = AsScriptingHelper.ShortPath(allFiles[i]);
					if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, shortPath, (i + 1) * 1.0f / allFiles.Length))
					{
						EditorUtility.ClearProgressBar();
						return false;
					}
					parser(shortPath);					
				}
				EditorUtility.ClearProgressBar();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				EditorUtility.ClearProgressBar();
			}
			return true;
		}

		protected static string GetGameObjectPath(Transform transform, Transform until = null)
		{
			if (transform.parent == null || transform == until)
				return transform.name;
			var fullPath = GetGameObjectPath(transform.parent, until) + "/" + transform.name;
			//in Unity 2018.4 and later, opening UI prefabs would create a temp canvas
			if (fullPath.StartsWith("Canvas (Environment)"))
				fullPath = fullPath.Substring(21);
			return fullPath;
		}

		protected static GameObject GetGameObject(GameObject go, string fullName)
		{
			if (go.name == fullName)
				return go;
			var names = fullName.Split('/');
			return go.name != names[0] ? null : GetGameObject(go, names, 1);
		}

		private static GameObject GetGameObject(GameObject go, string[] names, int index)
		{
			if (index > names.Length) return null;

			foreach (Transform child in go.transform)
			{
				if (child.gameObject.name == names[index])
				{
					return index == names.Length - 1 ? child.gameObject : GetGameObject(child.gameObject, names, index + 1);
				}
			}

			return null;
		}

		protected static GameObject GetRootGameObject(Transform trans)
		{
			return trans.parent ? GetRootGameObject(trans.parent) : trans.gameObject;
		}

		#region AssetOperations
		internal void SaveSelectedAssets(IEnumerable<string> assetPaths, Action<string> parser)
		{
			ReadData();
			foreach (var assetPath in assetPaths)
			{
				var xAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
				if (xAsset != null)
					xAsset.Remove();
				parser(assetPath);
			}
			AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
		}
		
		internal void ExportSelectedAssets(IEnumerable<string> assetPaths, Action<string> parser)
		{
			CleanUp();
			var xmlPath = EditorUtility.SaveFilePanel("Export to", XmlDocDirectory, "Selection.xml", "xml");
			if (string.IsNullOrEmpty(xmlPath)) return;
			foreach (var assetPath in assetPaths)
			{
				parser(assetPath);
			}
			AsScriptingHelper.WriteXml(xmlPath, XRoot);
			if (EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " items!", "Open", "OK"))
				Process.Start(xmlPath);
		}
		
		internal void RevertSelectedAssets(IEnumerable<string> assetPaths, Func<XElement, bool> importer)
		{
			ReadData();
			var edited = false;
			foreach (var assetPath in assetPaths)
			{
				var xAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
				if (importer(xAsset))
					edited = true;
			}
			if (edited)
				AssetDatabase.SaveAssets();
		}
		
		internal void RemoveSelectedAssets(IEnumerable<string> assetPaths, Action<string> remover)
		{
			ReadData();
			foreach (var assetPath in assetPaths)
			{
				remover(assetPath);
				var xAsset = XRoot.Elements().FirstOrDefault(x => assetPath == AsScriptingHelper.GetXmlAttribute(x, "AssetPath"));
				if (xAsset != null)
					xAsset.Remove();
			}
			AsScriptingHelper.WriteXml(DefaultXmlPath, XRoot);
			AssetDatabase.SaveAssets();
		}
		#endregion
		
		#region TypeCast
		protected static bool ImportFloat(ref float field, string s)
		{
			var value = AsScriptingHelper.StringToFloat(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}
        
		protected static bool ImportInt(ref int field, string s)
		{
			var value = AsScriptingHelper.StringToInt(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		protected static bool ImportBool(ref bool field, string s)
		{
			var value = AsScriptingHelper.StringToBool(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		protected static bool ImportVector3(ref Vector3 field, string s)
		{
			var value = AsScriptingHelper.StringToVector3(s);
			if (Mathf.Abs(field.magnitude - value.magnitude) > 0.01f)
			{
				field = value;
				return true;
			}
			return false;
		}
		
		protected static bool ImportEnum<T>(ref T field, string xComponent) where T: struct, IComparable
		{
			try
			{
				var value = (T) Enum.Parse(typeof(T), xComponent);
				if (!field.Equals(value))
				{
					field = value;
					return true;
				}
			}
#pragma warning disable 168
			catch (Exception e)
#pragma warning restore 168
			{
				Debug.LogError("Import failed: Can't find option " + xComponent + " in enum " + typeof(T).Name);
			}
			return false;
		}				
		#endregion
	}		
}