using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Editor;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class CharacterComponentSync : EditorWindow
    {
        private string _prefabFolder = "";
        private GameObject _samplePrefab;
        private string _sampleCharacterName = "";
        private bool _saveToXml = true;

        public void OnGUI()
        {
            AsGuiDrawer.DisplaySearchPath(ref _prefabFolder);
            _samplePrefab = (GameObject) EditorGUILayout.ObjectField("Sample Prefab", _samplePrefab, typeof(GameObject), false);
            _sampleCharacterName = EditorGUILayout.TextField("Character Name", _sampleCharacterName);
            _saveToXml = EditorGUILayout.Toggle("Save to Xml", _saveToXml);
            if (GUILayout.Button("Run"))
                Run();
        }

        private void Run()
        {
            if (!_samplePrefab)
            {
                EditorUtility.DisplayDialog("Error", "Please select a sample prefab!", "OK");
                return;
            }

            var sampleLoadBank = _samplePrefab.GetComponent<LoadBank>();
            var sampleEffectSound = _samplePrefab.GetComponent<EffectSound>();

            if (!sampleLoadBank && !sampleEffectSound)
            {
                EditorUtility.DisplayDialog("Error", "Please use a prefab with LoadBank or EffectSound!", "OK");
                return;
            }

            var prefabNamePrefix = _samplePrefab.name.Substring(0, _samplePrefab.name.IndexOf(_sampleCharacterName, StringComparison.Ordinal));
            var prefabNameSuffix = _samplePrefab.name.Substring( _samplePrefab.name.IndexOf(_sampleCharacterName, StringComparison.Ordinal) + _sampleCharacterName.Length);
            var prefabPaths = Directory.GetFiles(_prefabFolder, "*.prefab", SearchOption.AllDirectories);
            foreach (var prefabPath in prefabPaths)
            {
                var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                if (prefabName == _samplePrefab.name) continue;
                var prefabCharacterName = prefabName.Replace(prefabNamePrefix, "").Replace(prefabNameSuffix, "");
                prefabCharacterName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(prefabCharacterName);
                if (!prefabName.StartsWith(prefabNamePrefix) || !prefabName.EndsWith(prefabNameSuffix)) continue;
                var prefabPathShort = AsScriptingHelper.ShortPath(prefabPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathShort);
                if (!prefab) continue;
                
                if (sampleLoadBank && sampleLoadBank.Banks.Length > 0)
                {
                    var sampleBank = sampleLoadBank.Banks[0];
                    var prefabLoadBank = AsUnityHelper.GetOrAddComponent<LoadBank>(prefab);
                    var bankName = sampleLoadBank.Banks[0].Name.Replace(_sampleCharacterName, prefabCharacterName);
                    var newBankRef = new LoadBankReference(bankName)
                    {
                        LoadFinishEvents = sampleBank.LoadFinishEvents.Select(evt => new AudioEventReference(evt.Name.Replace(_sampleCharacterName, prefabCharacterName)) {Type = evt.Type}).ToArray(),
                        UnloadOnDisable = sampleBank.UnloadOnDisable
                    };
                    prefabLoadBank.Banks = new[] {newBankRef};
                    if (_saveToXml)
                        AsComponentBackup.Instance.UpdateXmlFromComponent(prefabPathShort, prefabLoadBank);
                }
                
                if (sampleEffectSound && sampleEffectSound.EnableEvents.Length > 0)
                {
                    var sampleEvent = sampleEffectSound.EnableEvents[0];
                    var prefabEffectSound = AsUnityHelper.GetOrAddComponent<EffectSound>(prefab);
                    var eventName = sampleEffectSound.EnableEvents[0].Name.Replace(_sampleCharacterName, prefabCharacterName);
                    var newEventRef = new PostEventReference(eventName)
                    {
                        Type = sampleEvent.Type,
                        Action = sampleEvent.Action, 
                        FadeTime = sampleEvent.FadeTime
                    };
                    prefabEffectSound.EnableEvents = new[] {newEventRef};
                    if (_saveToXml)
                        AsComponentBackup.Instance.UpdateXmlFromComponent(prefabPathShort, prefabEffectSound);
                }
                AsComponentBackup.SaveComponentAsset(prefab, prefabPathShort);
            }
        }

    }
}