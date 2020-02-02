using System;
using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
    public enum BankLoadStatus
    {
        NotLoaded,
        Loading,
        Loaded,
        Unloading,
        NotFound
    }

    internal class BankLoadData
    {
        internal SoundBank Bank;
        internal BankLoadStatus LoadStatus = BankLoadStatus.NotLoaded;
        internal int Count;
    }

    public static class BankManager
    {
        private static readonly Dictionary<string, BankLoadData> _banks = new Dictionary<string, BankLoadData>();

        #region Load
        internal static void LoadBank(string bankName, Action onLoadFinished = null, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_banks.ContainsKey(bankName))
                _banks[bankName] = new BankLoadData();

            if (_banks[bankName].LoadStatus == BankLoadStatus.Loading)
            {
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank is already loading");
                return;
            }
            
            if (_banks[bankName].Count == 0) //if bank is not loaded
            {
                _banks[bankName].LoadStatus = BankLoadStatus.Loading;
                AsAssetLoader.LoadBank(bankName, (bank, status) =>
                {
                    _banks[bankName].LoadStatus = status;
                    switch (status)
                    {
                        case BankLoadStatus.Loaded:
                            _banks[bankName].Count = 1;
                            _banks[bankName].Bank = bank;
                            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source);
                            onLoadFinished?.Invoke();
                            break;
                        case BankLoadStatus.NotFound:
                            AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank not found");
                            break;
                    }
                });
            }
            else //if more load requests are received
            {
                _banks[bankName].Count++;
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Sound Bank load counter: " + _banks[bankName]);
            }
        }
        #endregion
        
        #region Unload
        internal static void UnloadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_banks.ContainsKey(bankName))
                _banks[bankName] = new BankLoadData();

            switch (_banks[bankName].Count)
            {
                case 0:
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Sound Bank is already unloaded");
                    break;
                case 1:
                    if (_banks[bankName].LoadStatus == BankLoadStatus.NotFound)
                        AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank not found");
                    else
                    {
                        _banks[bankName].LoadStatus = BankLoadStatus.Unloading;
                        AsAssetLoader.UnloadBank(_banks[bankName].Bank);
                        _banks[bankName].Bank = null;
                        _banks[bankName].LoadStatus = BankLoadStatus.NotLoaded;
                        _banks[bankName].Count = 0;
                        AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source);
                    }
                    break;
                default:
                    _banks[bankName].Count--;
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Sound Bank unload counter: " + _banks[bankName]);
                    break;
            }
        }
        
        internal static void UnloadBanks(string nameFilter)
        {
            var loadedBanks = new List<string>(_banks.Keys);
            foreach (var bank in loadedBanks)
            {
                if (bank.Contains(nameFilter))
                    UnloadBank(bank);
            }
        }
        #endregion

        #region Reload
        public static void RefreshAllBanks()
        {
            foreach (var bank in _banks)
            {
                AsAssetLoader.UnloadBank(bank.Value.Bank);
                LoadBank(bank.Key);
            }
        }
        #endregion
    }
}