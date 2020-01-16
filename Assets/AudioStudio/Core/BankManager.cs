using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
    public static class BankManager
    {
        private static readonly Dictionary<string, int> _loadedBankList = new Dictionary<string, int>();

        #region Load
        internal static void LoadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_loadedBankList.ContainsKey(bankName) || _loadedBankList[bankName] == 0)
            {
                if (AsAssetLoader.LoadBank(bankName))
                {
                    _loadedBankList[bankName] = 1;
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source);
                }
                else
                    AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank not found");
            }
            else
            {
                _loadedBankList[bankName]++;
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Sound Bank load counter: " + _loadedBankList[bankName]);
            }
        }
        #endregion
        
        #region Unload
        internal static void UnloadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_loadedBankList.ContainsKey(bankName) || _loadedBankList[bankName] == 0)
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Sound Bank is already unloaded");
            else if (_loadedBankList[bankName] == 1)
            {
                if (AsAssetLoader.UnloadBank(bankName))
                {
                    _loadedBankList[bankName] = 0;
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source);
                }
                else
                    AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank not found");
            }
            else
            {
                _loadedBankList[bankName]--;
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Sound Bank unload counter: " + _loadedBankList[bankName]);
            }
        }
        
        internal static void UnloadBanks(string nameFilter)
        {
            var loadedBanks = new List<string>(_loadedBankList.Keys);
            foreach (var bank in loadedBanks)
            {
                if (bank.Contains(nameFilter))
                    UnloadBank(bank);
            }
        }
        #endregion

        #region Validation
        private static bool IsLoaded(string bankName)
        {
            return _loadedBankList.ContainsKey(bankName) && _loadedBankList[bankName] > 0;
        }
        #endregion

        #region Reload
        public static void RefreshAllBanks()
        {
            foreach (var bank in _loadedBankList.Keys)
            {
                AsAssetLoader.UnloadBank(bank);
                AsAssetLoader.LoadBank(bank);
            }
        }
        #endregion
    }
}