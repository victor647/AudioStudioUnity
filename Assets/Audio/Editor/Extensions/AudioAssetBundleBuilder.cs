using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AudioStudio.Configs;

namespace AudioStudio
{
    public static class AudioAssetBundleBuilder
    {
        [MenuItem("AudioStudio/Build AssetBundles")]
        public static void BuildAssetBundles()
        {														
            SetLabels<SoundBank>(AudioPathSettings.SoundBanksPath, "bank");
            SetLabels<MusicContainer>(AudioPathSettings.MusicEventsPath, "music");
            SetLabels<VoiceEvent>(AudioPathSettings.VoiceEventsPath, "voice");
            AssetDatabase.RemoveUnusedAssetBundleNames();	
            AssetDatabase.SaveAssets();
			
            var buildPath = Directory.GetParent(Application.dataPath) + "/AssetBundles";
            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);			
            BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows);			
        }

        private static void SetLabels<T>(string loadPath, string bundleType) where T: ScriptableObject
        {
            string [] resourcePaths = Directory.GetFiles(Application.dataPath + loadPath, "*.asset", SearchOption.AllDirectories);
            foreach (var resourcePath in resourcePaths)
            {
                var shortPath = resourcePath.Substring(resourcePath.IndexOf("Assets/", StringComparison.Ordinal));
                var importer = AssetImporter.GetAtPath(shortPath);				
                var asset = AssetDatabase.LoadAssetAtPath<T>(shortPath);
                if (!importer) continue;
                importer.assetBundleName = "audio/" + bundleType + "/" + asset.name + ".asset";				
            }
			
        }
    }
}