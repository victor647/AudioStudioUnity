using System;
using System.Collections.Generic;
using System.Threading;
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

    public static class BankManager
    {
        /// <summary>
        /// Store the bank load status and counters.
        /// </summary>
        private class BankLoadData
        {
            internal SoundBank Bank;
            internal BankLoadStatus LoadStatus = BankLoadStatus.NotLoaded;
            internal bool UseCounter;
            internal int LoadCount;

            internal void DoLoad(SoundBank bank)
            {
                UseCounter = bank.UseLoadCounter;
                LoadedBankList.Add(bank.name);
                LoadCount++;
                Bank = bank;
            }

            internal void DoUnload()
            {
                _mutex.WaitOne();
                LoadStatus = BankLoadStatus.Unloading;
                LoadCount = 0;
                LoadedBankList.Remove(Bank.name);
                AsAssetLoader.UnloadBank(Bank);
                Bank = null;
                LoadStatus = BankLoadStatus.NotLoaded;
                _mutex.ReleaseMutex();
            }
        }
        
        private static readonly Dictionary<string, BankLoadData> _banks = new Dictionary<string, BankLoadData>();
        public static readonly List<string> LoadedBankList = new List<string>();
        private static readonly Mutex _mutex = new Mutex();

        #region Load
        internal static void LoadBank(SoundBank bank, GameObject source = null)
        {
            // create new BankLoadData if this bank has never been loaded
            if (!_banks.ContainsKey(bank.name))
                _banks[bank.name] = new BankLoadData();
            var bankLoadData = _banks[bank.name];
            
            switch (bankLoadData.LoadStatus)
            {
                case BankLoadStatus.NotLoaded:
                    _mutex.WaitOne();
                    bankLoadData.LoadStatus = BankLoadStatus.Loading;
                    AsAssetLoader.DoLoadBank(bank);
                    bankLoadData.DoLoad(bank);
                    bankLoadData.LoadStatus = BankLoadStatus.Loaded;
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Load, AudioTriggerSource.LoadBank, bank.name, source);
                    _mutex.ReleaseMutex();
                    break;
                case BankLoadStatus.Loaded:
                    if (bankLoadData.UseCounter)
                    {
                        bankLoadData.LoadCount++;
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, AudioTriggerSource.LoadBank, bank.name, source, "Load counter: " + bankLoadData.LoadCount);
                    }
                    else
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, AudioTriggerSource.LoadBank, bank.name, source, "Bank already loaded");
                    break;
            }
        }
        
        internal static void LoadBank(string bankName, Action onLoadFinished = null, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_banks.ContainsKey(bankName))
                _banks[bankName] = new BankLoadData();
            var bankLoadData = _banks[bankName];
            
            switch (bankLoadData.LoadStatus)
            {
                case BankLoadStatus.NotFound:
                    AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank not found");
                    break;
                case BankLoadStatus.Loading:
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank already loading");
                    break;
                case BankLoadStatus.NotLoaded:
                    _mutex.WaitOne();
                    bankLoadData.LoadStatus = BankLoadStatus.Loading;
                    AsAssetLoader.LoadBank(bankName, (bank, status) =>
                    {
                        bankLoadData.LoadStatus = status;
                        switch (status)
                        {
                            case BankLoadStatus.Loaded:
                                bankLoadData.DoLoad(bank);
                                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source);
                                onLoadFinished?.Invoke();
                                break;
                            case BankLoadStatus.NotFound:
                                AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank not found");
                                break;
                        }
                        _mutex.ReleaseMutex();
                    });
                    break;
                case BankLoadStatus.Loaded:
                    // increment counter if already loaded
                    if (bankLoadData.UseCounter) 
                    {
                        bankLoadData.LoadCount++;
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Load counter: " + bankLoadData.LoadCount);
                    }
                    else
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Load, trigger, bankName, source, "Bank already loaded");
                    onLoadFinished?.Invoke();
                    break;
            }
        }
        #endregion
        
        #region Unload
        internal static void UnloadBank(SoundBank bank, GameObject source = null)
        {
            if (!_banks.ContainsKey(bank.name))
                _banks[bank.name] = new BankLoadData();
            var bankLoadData = _banks[bank.name];
            
            switch (bankLoadData.LoadStatus)
            {
                case BankLoadStatus.NotLoaded:
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, AudioTriggerSource.LoadBank, bank.name, source, "Bank already unloaded");
                    break;
                case BankLoadStatus.Loaded:
                    // decrement counter if still more than one
                    if (bankLoadData.UseCounter && bankLoadData.LoadCount > 1) 
                    {
                        bankLoadData.LoadCount--;
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, AudioTriggerSource.LoadBank, bank.name, source, "Remaining counter: " + bankLoadData.LoadCount);
                    }
                    else
                    {
                        bankLoadData.DoUnload();
                        AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Unload, AudioTriggerSource.LoadBank, bank.name, source);
                    }
                    break;
            }
        }
        
        internal static void UnloadBank(string bankName, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (!_banks.ContainsKey(bankName))
                _banks[bankName] = new BankLoadData();
            var bankLoadData = _banks[bankName];
            switch (bankLoadData.LoadStatus)
            {
                case BankLoadStatus.NotFound:
                    AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank not found");
                    break;
                case BankLoadStatus.Unloading:
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank already unloading");
                    break;
                case BankLoadStatus.NotLoaded:
                    AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Bank already unloaded");
                    break;
                case BankLoadStatus.Loading:
                case BankLoadStatus.Loaded:
                    if (bankLoadData.UseCounter && bankLoadData.LoadCount > 1)
                    {
                        bankLoadData.LoadCount--;
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source, "Remaining counter: " + bankLoadData.LoadCount);
                    }
                    else
                    {
                        bankLoadData.DoUnload();
                        AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SoundBank, AudioAction.Unload, trigger, bankName, source);
                    }
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
        
        public static void RefreshVoiceBanks()
        {
            foreach (var bank in _banks)
            {
                if (bank.Key.StartsWith("Voice_"))
                {
                    AsAssetLoader.UnloadBank(bank.Value.Bank);
                    LoadBank(bank.Key);
                }
            }
        }
        #endregion
    }
}