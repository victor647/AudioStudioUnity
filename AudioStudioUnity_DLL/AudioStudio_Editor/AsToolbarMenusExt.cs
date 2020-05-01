using System.IO;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Tools
{
    public static partial class AsToolbarMenus
    {
        private const string PerforcePort = "10.18.24.7:2222";
        private const string SubmitAudioStudioDescription = "【音频】更新AudioStudio库与配置备份";
        private const string SubmitAudioResourcesDescription = "【音频】更新音频资源";

        [MenuItem("AudioStudio/Tools/Set Commander Banks to Sync Mode")]
        public static void ChangeSoundBankToSyncMode()
        {
            var searchPath = Application.dataPath + "/Resources/Role";
            var filePaths = Directory.GetFiles(searchPath, "*.prefab", SearchOption.AllDirectories);
            for (var i = 0; i < filePaths.Length; i++)
            {
                var shortPath = AsScriptingHelper.ShortPath(filePaths[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Changing Bank Mode", shortPath, i / (filePaths.Length * 1f))) break;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(shortPath);
                if (!prefab) continue;
                var loadBank = prefab.GetComponentInChildren<LoadBank>();
                if (!loadBank || !loadBank.AsyncMode || loadBank.AsyncBanks.Length == 0) continue;
                //if (!loadBank) continue;
                if (!filePaths[i].EndsWith("_high.prefab")) continue;
                var bankPath = AsScriptingHelper.CombinePath("Assets", AudioPathSettings.Instance.SoundBanksPath, loadBank.AsyncBanks[0].Name + ".asset");
                var soundBank = AssetDatabase.LoadAssetAtPath<SoundBank>(bankPath);
                if (!soundBank) continue;
                var events = loadBank.AsyncBanks[0].LoadFinishEvents;
                var effectSound = AsUnityHelper.GetOrAddComponent<EffectSound>(loadBank.gameObject);
                effectSound.EnableEvents = events.Select(evt => new PostEventReference(evt.Name)).ToArray();
                loadBank.SyncBanks = new[] {soundBank};
                loadBank.AsyncBanks = new LoadBankReference[0];
                loadBank.AsyncMode = false;
                EditorUtility.SetDirty(prefab);
                AsComponentBackup.Instance.UpdateXmlFromComponent(shortPath, loadBank);
                AsComponentBackup.Instance.UpdateXmlFromComponent(shortPath, effectSound);
            }
            EditorUtility.ClearProgressBar();
        }
    }
}