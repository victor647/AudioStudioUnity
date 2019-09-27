using System;
using System.Diagnostics;
using System.IO;
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
	
	public abstract class AsSearchers : EditorWindow
	{		
		protected internal bool IncludeA = true;
		protected internal bool IncludeB = true;
		protected static string SearchPath = "Assets";

		protected string XmlDocPath => AudioPathSettings.EditorConfigPath;
		protected int EditedCount;
		protected int TotalCount;
		protected XElement XRoot = new XElement("Root");

		protected virtual string DefaultXmlPath => "";

		internal void OpenXmlFile()
		{
			if (File.Exists(DefaultXmlPath))
				Process.Start(DefaultXmlPath);
			else
				EditorUtility.DisplayDialog("Error", "Default xml file does not exist!", "OK");
		}

		//reset fields to prevent duplicate
		protected virtual void CleanUp()
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
				AudioUtility.WriteXml(DefaultXmlPath, XRoot);
			}
		}

		protected static bool FindFiles(Action<string> parser, string progressBarTitle, string extension)
		{
			try
			{
				EditorUtility.DisplayCancelableProgressBar(progressBarTitle, "Loading assets...", 0);
				string[] allFiles = Directory.GetFiles(SearchPath, extension, SearchOption.AllDirectories);
				for (var i = 0; i < allFiles.Length; i++)
				{
					var shortPath = AudioUtility.ShortPath(allFiles[i]);
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
			return GetGameObjectPath(transform.parent, until) + "/" + transform.name;
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

		protected static string GetFullAssetPath(XElement node)
		{
			return AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(node, "Path"), AudioUtility.GetXmlAttribute(node, "Asset"));
		}
	}		
}