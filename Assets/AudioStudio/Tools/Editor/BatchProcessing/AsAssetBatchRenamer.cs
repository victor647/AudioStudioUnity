using System.IO;
using AudioStudio.Tools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AsAssetBatchRenamer : EditorWindow
{
    private string _prefix = "";
    private string _suffix = "";
    private string _find = "";
    private string _replace = "";

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Add Prefix:");
        _prefix = GUILayout.TextField(_prefix);
        EditorGUILayout.LabelField("Add Suffix:");
        _suffix = GUILayout.TextField(_suffix);
        EditorGUILayout.LabelField("Find:");
        _find = GUILayout.TextField(_find);
        EditorGUILayout.LabelField("Replace with:");
        _replace = GUILayout.TextField(_replace);
        if (GUILayout.Button("Rename!"))
        {
            foreach (var asset in Selection.objects)
            {
                var originalPath = AssetDatabase.GetAssetPath(asset);
                var oldName = Path.GetFileNameWithoutExtension(originalPath);
                var extension = Path.GetExtension(originalPath);
                if (!string.IsNullOrEmpty(_find) && !string.IsNullOrEmpty(_replace))
                    oldName = oldName.Replace(_find, _replace);
                var newName = _prefix + oldName + _suffix + extension;
                AssetDatabase.RenameAsset(originalPath, newName);
            }
        }
    }
}
