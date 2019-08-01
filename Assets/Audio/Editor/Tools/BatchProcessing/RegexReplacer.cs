using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
	public class RegexReplacer : EditorWindow
	{

		private string _regexPattern;
		private string _findInResult;
		private string _replaceInResult;

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Regex Pattern", GUILayout.Width(100));
			_regexPattern = EditorGUILayout.TextField(_regexPattern);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Find in Result", GUILayout.Width(100));
			_findInResult = EditorGUILayout.TextField(_findInResult);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Replace", GUILayout.Width(100));
			_replaceInResult = EditorGUILayout.TextField(_replaceInResult);
			EditorGUILayout.EndHorizontal();

			if (GUILayout.Button("Open File")) OpenFile();
			if (GUILayout.Button("Find & Replace")) FindAndReplace();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Save")) SaveFile(false);
			if (GUILayout.Button("Save As")) SaveFile(true);
			EditorGUILayout.EndHorizontal();
		}

		private string _text;
		private string _filePath;
		private string _fileName;
		private string _extension;

		private void OpenFile()
		{
			_filePath = EditorUtility.OpenFilePanel("Import from", Application.dataPath, "*");
			if (string.IsNullOrEmpty(_filePath)) return;
			_fileName = Path.GetFileNameWithoutExtension(_filePath);
			_extension = Path.GetExtension(_filePath);
			_text = File.ReadAllText(_filePath);

		}

		private void FindAndReplace()
		{
			var matches = Regex.Matches(_text, _regexPattern);
			foreach (Match match in matches)
			{
				var oldValue = match.Value;
				var newValue = match.Value.Replace(_findInResult, _replaceInResult);
				_text = _text.Replace(oldValue, newValue);
			}

			EditorUtility.DisplayDialog("Result", "Replaced " + matches.Count + " occurrences!", "OK");
		}

		private void SaveFile(bool saveAs)
		{
			if (saveAs)
			{
				var filePath = EditorUtility.SaveFilePanel("Export to", Application.dataPath, _fileName + "_edited", _extension);
				File.WriteAllText(filePath, _text);
			}
			else
			{
				File.WriteAllText(_filePath, _text);
			}
		}
	}
}