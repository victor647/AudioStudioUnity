using System;
using System.IO;
using AudioStudio.Editor;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
    internal class AsScriptReferenceUpdate : AsSearchers
    {
        private string _extension;
        private string _oldGuid;
        private string _newGuid;

        private void OnGUI()
        {
            _extension = EditorGUILayout.TextField("Asset Extension", _extension);
            _oldGuid = EditorGUILayout.TextField("Old Guid", _oldGuid);
            _newGuid = EditorGUILayout.TextField("New Guid", _newGuid);

            AsGuiDrawer.DisplaySearchPath(ref SearchPath);
            if (GUILayout.Button("Update!"))
            {
                CleanUp();
                FindFiles(UpdateAsset, "Updating Scenes", "*." + _extension);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Finished", string.Format("Updated {0} out of {1} Assets!", EditedCount, TotalCount), "OK");
            }
        }

        private void UpdateAsset(string filePath)
        {
            var path = Path.Combine(Application.dataPath, filePath.Substring(7));
            AsScriptingHelper.CheckoutLockedFile(path);
            var text = File.ReadAllText(path);
            var newText = text.Replace(_oldGuid, _newGuid);
            TotalCount++;
            if (text != newText)
            {
                File.WriteAllText(path, newText);
                EditedCount++;
            }
        }
    }
}