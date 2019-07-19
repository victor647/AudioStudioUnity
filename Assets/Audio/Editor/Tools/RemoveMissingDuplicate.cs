using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace AudioStudio
{
	public class RemoveMissingDuplicate : AsSearchers
	{
		private bool _edited;
		private MonoScript _script;				
		private Action<GameObject> _remover;

		private void OnGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Duplicate Script Type");
			_script = EditorGUILayout.ObjectField(_script, typeof(MonoScript), false) as MonoScript;
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			IncludeA = GUILayout.Toggle(IncludeA, "Search in prefabs");
			IncludeB = GUILayout.Toggle(IncludeB, "Search in scenes");
			GUILayout.EndHorizontal();

			AudioScriptGUI.DisplaySearchPath(ref SearchPath);

			if (GUILayout.Button("Remove Missing Scripts")) RemoveMissingScripts();
			if (GUILayout.Button("Remove Duplicate Scripts")) RemoveDuplicateScripts();
		}

		#region Missing
		private void RemoveMissingScripts()
		{
			TotalCount = 0;
			_remover = RemoveMissing;
			if (IncludeA) FindFiles(SearchPrefabs, "Searching Prefabs", "*.prefab");
			if (IncludeB) FindFiles(SearchScenes, "Searching Scenes", "*.unity");
			EditorUtility.DisplayDialog("Success", string.Format("Removed {0} missing scripts!", TotalCount), "OK");
		}

		private void SearchPrefabs(string filePath)
		{				
			var prefab = (GameObject) AssetDatabase.LoadAssetAtPath(filePath, typeof(GameObject));				
			_edited = false;				
			GetChild(prefab.transform);
			if (_edited)
			{
				TotalCount++;
				EditorUtility.SetDirty(prefab);
			}			
		}

		private void SearchScenes(string filePath)
		{
			EditorSceneManager.OpenScene(filePath);
			var scene = SceneManager.GetActiveScene();
			GameObject[] gameObjects = scene.GetRootGameObjects();
												
			_edited = false;				
			foreach (var gameObject in gameObjects)
			{											
				GetChild(gameObject.transform);			
			}

			if (_edited)
			{
				TotalCount++;
				EditorSceneManager.SaveScene(scene, scene.path, false);
			}																												
			EditorSceneManager.CloseScene(scene, false);
		}

		private void GetChild(Transform t)
		{
			_remover(t.gameObject);
			foreach (Transform child in t)
			{
				GetChild(child);
			}
		}	
		
		private void RemoveMissing(GameObject gameObject)
		{
			var serializedObject = new SerializedObject(gameObject);
			var property = serializedObject.FindProperty("m_Component");
			EditedCount = 0;
			Component[] components = gameObject.GetComponents<Component>();
			for (var i = 0; i < components.Length; i++)
			{
				if (components[i]) continue;
				_edited = true;
				property.DeleteArrayElementAtIndex(i - EditedCount);
				EditedCount++;
			}
			serializedObject.ApplyModifiedProperties();
		}
		#endregion
		
		#region Duplicate
		private void RemoveDuplicateScripts()
		{
			TotalCount = 0;
			_remover = RemoveDuplicate;
			if (IncludeA) FindFiles(SearchPrefabs, "Searching Prefabs", "*.prefab");
			if (IncludeB) FindFiles(SearchScenes, "Searching Scenes", "*.unity");
			EditorUtility.DisplayDialog("Success", string.Format("Removed {0} duplicate scripts!", TotalCount), "OK");
		}

		private void RemoveDuplicate(GameObject gameObject)
		{
			Component[] components = gameObject.GetComponents(_script.GetClass());
			if (components.Length > 1)
			for (var i = 1; i < components.Length; i++)
			{
				DestroyImmediate(components[i]);
			}			
		}
		#endregion
	}

}