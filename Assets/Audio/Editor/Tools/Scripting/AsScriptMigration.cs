using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using UnityEditor.VersionControl;

namespace AudioStudio.Tools
{
	public class AsScriptMigration : EditorWindow
	{
		private class FileComparisonData
		{
			public string SourceFilePath;
			public string TargetFilePath;
			public bool WillCopy = true;
			public FileInfo SourceFile;
			public FileInfo TargetFile;
		}
		
		private string _otherGameRootPath;
		private string[] _sourceScripts;
		private bool _migrateToOtherGame;
		private List<FileComparisonData> _changeList = new List<FileComparisonData>();
		private bool _selectAll = true;
		private bool _selectNew = true;
		private bool _selectLater = true;
		private bool _selectLarger = true;
		private bool _includeExtensions;
		
		private Vector2 _scrollPosition;		

		private void OnGUI()
		{
			GUILayout.Label("This tool migrates AudioStudio scripts from this game to another game or vice versa.");			                
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{				
				_otherGameRootPath = GUILayout.TextField(_otherGameRootPath);
				if (GUILayout.Button("Browse AudioStudio Folder of Another Game"))
					_otherGameRootPath = EditorUtility.OpenFolderPanel("Audio Folder", _otherGameRootPath, "");						
			}
								
			GUILayout.BeginHorizontal();
			_migrateToOtherGame = GUILayout.Toggle(_migrateToOtherGame, "Migrate from this game to another game");
			_includeExtensions = GUILayout.Toggle(_includeExtensions, "Include Extensions");
			if (GUILayout.Button("Search Script Difference")) SearchScriptDifference();			
			GUILayout.EndHorizontal();						
			
			if (_changeList.Count == 0) return;
			
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{				
				GUILayout.BeginHorizontal();
				GUILayout.Label("Script Name", GUILayout.Width(200));
				GUILayout.Label("Source Size", GUILayout.Width(80));
				GUILayout.Label("Source Modified", GUILayout.Width(120));
				GUILayout.Label("Target Size", GUILayout.Width(80));
				GUILayout.Label("Target Modified", GUILayout.Width(120));
				GUILayout.Label("Diff");
				GUILayout.EndHorizontal();
				_scrollPosition =  EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(680), GUILayout.Height(200));
				foreach (var data in _changeList)
				{
					GUILayout.BeginHorizontal();
					data.WillCopy = GUILayout.Toggle(data.WillCopy,  Path.GetFileName(data.SourceFilePath), GUILayout.Width(200));
					GUILayout.Label(data.SourceFile.Length.ToString(), GUILayout.Width(80));
					GUILayout.Label(data.SourceFile.LastWriteTime.ToShortDateString(), GUILayout.Width(120));					
					if (data.TargetFile != null)
					{
						GUILayout.Label(data.TargetFile.Length.ToString(), GUILayout.Width(80));
						GUILayout.Label(data.TargetFile.LastWriteTime.ToShortDateString(), GUILayout.Width(120));							
					}
					else
					{
						GUILayout.Label("File does not exist at target", GUILayout.Width(204));								
					}

					if (data.TargetFile != null && GUILayout.Button("...", GUILayout.Width(30)))
					{						
						Process.Start("p4merge", data.SourceFilePath + " " + data.TargetFilePath);						 
					}
					GUILayout.EndHorizontal();
				}
				EditorGUILayout.EndScrollView();
			}
			
			GUILayout.BeginHorizontal();
			if (_selectAll != GUILayout.Toggle(_selectAll, "Select All"))
			{
				_selectAll = SelectScripts(!_selectAll, 0);
				_selectNew = _selectLater = _selectLarger = _selectAll;
			}
			if (_selectNew != GUILayout.Toggle(_selectNew, "Select New Scripts")) _selectNew = SelectScripts(!_selectNew, 1);	
			if (_selectLater != GUILayout.Toggle(_selectLater, "Select Later Modified")) _selectLater = SelectScripts(!_selectLater, 2);
			if (_selectLarger != GUILayout.Toggle(_selectLarger, "Select Larger")) _selectLarger = SelectScripts(!_selectLarger, 3);									
			GUILayout.EndHorizontal();
			if (GUILayout.Button("Migrate (This operation can't be undone!)")) Migrate();
		}

		private bool SelectScripts(bool select, int action)
		{
			foreach (var data in _changeList)
			{
				if (action == 0) data.WillCopy = select;
				if (data.TargetFile == null)
				{
					if (action == 1) data.WillCopy = select;
				}
				else if (action == 2)
				{
					if (data.SourceFile.LastWriteTime > data.TargetFile.LastWriteTime) data.WillCopy = select;
				}	
				else if (action == 3)
				{
					if (data.SourceFile.Length > data.TargetFile.Length) data.WillCopy = select;
				}							
			}

			return select;
		}
		
		private void SearchScriptDifference()
		{
			if (string.IsNullOrEmpty(_otherGameRootPath))
			{
				EditorUtility.DisplayDialog("Error", "Other game not located!", "OK");
				return;
			}
			GetAllScripts();
			_changeList.Clear();
			foreach (var path in _sourceScripts)
			{
				if (!_includeExtensions && path.Contains("Extensions")) continue;
				var cd = new FileComparisonData
				{
					SourceFilePath = path,
					TargetFilePath = _migrateToOtherGame ? 
						path.Replace(AudioPathSettings.AudioStudioLibraryPath, _otherGameRootPath) : 
						path.Replace(_otherGameRootPath, AudioPathSettings.AudioStudioLibraryPath),					 
					SourceFile = new FileInfo(path)
				};

				if (File.Exists(cd.TargetFilePath))
				{					
					cd.TargetFile = new FileInfo(cd.TargetFilePath);	
					if (cd.SourceFile.Length != cd.TargetFile.Length) _changeList.Add(cd);					
				} else _changeList.Add(cd);											
			}
			EditorUtility.DisplayDialog("Check Result", _changeList.Count == 0 ? "All files are identical!" : 
				_changeList.Count + " files are different.", "OK");
		}

		private void GetAllScripts()
		{
			_sourceScripts = Directory.GetFiles(_migrateToOtherGame ? AudioPathSettings.AudioStudioLibraryPath : 
				_otherGameRootPath, "*.cs", SearchOption.AllDirectories);
		}
		
		private void Migrate()
		{
			if (_changeList.Count == 0)
			{
				EditorUtility.DisplayDialog("Invalid Operation", "Please check for difference first!", "OK");
				return;
			}

			var copyCount = 0;
			foreach (var data in _changeList)
			{
				if (!data.WillCopy) continue;
				var folderPath = Path.GetDirectoryName(data.TargetFilePath);
				if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
				if (data.TargetFile != null && data.TargetFile.IsReadOnly)
				{
					if (Provider.isActive)
						Provider.Checkout(data.TargetFile.FullName, CheckoutMode.Asset);					
					data.TargetFile.IsReadOnly = false;
				}
				File.Copy(data.SourceFilePath, data.TargetFilePath, true);
				copyCount++;
			}

			EditorUtility.DisplayDialog("Success!", "Copied " + copyCount + " files to target!", "OK");
			_changeList.Clear();
			AssetDatabase.Refresh();
			Close();
		}
	}
}
