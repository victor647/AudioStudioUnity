using System;
using System.IO;
using AudioStudio.Editor;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
	internal class AsFieldUpgrade : AsSearchers
	{		
		private MonoScript _script;
		private Type _type;
		private string _oldString;
		private string _newString;
		
		private void Upgrade(string filePath)
		{
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);						
			if (!go.GetComponentInChildren(_type)) return;
			var path = Path.Combine(Application.dataPath, filePath.Substring(7));					
			var text = File.ReadAllText(path);			
			var newText = text.Replace(_oldString, _newString);			
			if (text != newText)
			{				
				AsScriptingHelper.CheckoutLockedFile(path);
				File.WriteAllText(path, newText);
				EditorUtility.SetDirty(go);
			}						
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Script Class To Upgrade");
			_script = EditorGUILayout.ObjectField(_script, typeof(MonoScript), false) as MonoScript;
			GUILayout.EndHorizontal();
						 			
			_oldString = EditorGUILayout.TextField("Old Field Name", _oldString);
			_newString = EditorGUILayout.TextField("New Field Name", _newString);
			
			GUILayout.BeginHorizontal();
			IncludeA = GUILayout.Toggle(IncludeA, "Search in prefabs");
			IncludeB = GUILayout.Toggle(IncludeB, "Search in scenes");
			GUILayout.EndHorizontal();
			
			AsGuiDrawer.DisplaySearchPath(ref SearchPath);
			if (GUILayout.Button("Replace!")) Replace();
		}

		private void Replace()
		{
			if (_script == null)
			{
				EditorUtility.DisplayDialog("Error", "Please Select a Script!", "OK");
			}
			TotalCount = 0;
			_type = _script.GetClass();
			if (IncludeA) FindFiles(Upgrade, "Upgrading Prefabs", "*.prefab");
			if (IncludeB) FindFiles(Upgrade, "Upgrading Scenes", "*.unity");
			AssetDatabase.SaveAssets();
			EditorUtility.DisplayDialog("Finished", "Upgraded " + TotalCount + " Assets!", "OK");
		}				
	}
}