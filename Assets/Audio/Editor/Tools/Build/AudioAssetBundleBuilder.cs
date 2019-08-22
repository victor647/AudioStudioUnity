using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using AudioStudio.Configs;
using AudioStudio.Tools;

namespace AudioStudio
{
    public static class AudioAssetBundleBuilder
    {
        public static void BuildAssetBundles()
        {														
            SetLabels<SoundBank>(AudioPathSettings.SoundBanksPath, "bank");
            SetLabels<MusicContainer>(AudioPathSettings.MusicEventsPath, "music");
            SetLabels<VoiceEvent>(AudioPathSettings.VoiceEventsPath, "voice");
            AssetDatabase.RemoveUnusedAssetBundleNames();	
            AssetDatabase.SaveAssets();
			
            var buildPath = Path.Combine(Application.dataPath, "../AssetBundles");
            AudioUtility.CheckDirectoryExist(buildPath);		
            BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows);			
        }

        private static void SetLabels<T>(string loadPath, string bundleType) where T: ScriptableObject
        {
            string [] resourcePaths = Directory.GetFiles(AudioUtility.CombinePath(Application.dataPath, loadPath), "*.asset", SearchOption.AllDirectories);
            foreach (var resourcePath in resourcePaths)
            {
                var shortPath = AudioUtility.ShortPath(resourcePath);
                var importer = AssetImporter.GetAtPath(shortPath);				
                var asset = AssetDatabase.LoadAssetAtPath<T>(shortPath);
                if (!importer) continue;
                importer.assetBundleName = "audio/" + bundleType + "/" + asset.name + ".asset";				
            }
			
        }
    }
}