using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AudioStudio
{
	public class AsFieldUpgrade : AsSearchers
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
				CheckoutLocked(path);
				File.WriteAllText(path, newText);
				EditorUtility.SetDirty(go);
			}						
		}

		private void OnGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Script To Upgrade");
			_script = EditorGUILayout.ObjectField(_script, typeof(MonoScript), false) as MonoScript;
			GUILayout.EndHorizontal();
						 			
			_oldString = GUILayout.TextField(_oldString);
			_newString = GUILayout.TextField(_newString);
			
			GUILayout.BeginHorizontal();
			IncludeA = GUILayout.Toggle(IncludeA, "Search in prefabs");
			IncludeB = GUILayout.Toggle(IncludeB, "Search in scenes");
			GUILayout.EndHorizontal();
			
			AudioScriptGUI.DisplaySearchPath(ref SearchPath);
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
			if (IncludeA) FindFiles(Upgrade, "Exporting Prefabs", "*.prefab");
			if (IncludeB) FindFiles(Upgrade, "Exporting Scenes", "*.unity");
			AssetDatabase.SaveAssets();
		}				
	}
}