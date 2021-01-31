using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace AudioStudio.Components
{    
    [AddComponentMenu("AudioStudio/Load Bank")]
    [DisallowMultipleComponent]
    public class LoadBank : AudioEmitter3D
    {
        public bool AsyncMode = true;
        public SoundBank[] SyncBanks = new SoundBank[0];
        [FormerlySerializedAs("Banks")] public LoadBankReference[] AsyncBanks = new LoadBankReference[0];

        public override void Activate(int index = 0)
        {
            if (AsyncMode)
            {
                for (var i = 0; i < AsyncBanks.Length; i++)
                {
                    if (index == 0 || index == i + 1)
                        AsyncBanks[i].Load(gameObject, AudioTriggerSource.LoadBank);
                }
            }
            else
            {
                for (var i = 0; i < SyncBanks.Length; i++)
                {
                    var bank = SyncBanks[i];
                    if (index == 0 || index == i + 1)
                    {
                        if (bank.IsValid())
                            BankManager.LoadBank(bank, gameObject);
                    }
                }
            }
        }

        public override void Deactivate(int index = 0)
        {
            if (AsyncMode)
            {
                for (var i = 0; i < AsyncBanks.Length; i++)
                {
                    if (index == 0 || index == i + 1)
                    {
                        var bank = AsyncBanks[i];
                        if (bank.UnloadOnDisable)
                            bank.Unload(gameObject, AudioTriggerSource.LoadBank);
                    }
                }
            }
            else
            {
                for (var i = 0; i < SyncBanks.Length; i++)
                {
                    if (index == 0 || index == i + 1)
                    {
                        var bank = SyncBanks[i];
                        if (bank.IsValid())
                            BankManager.UnloadBank(bank, gameObject);
                    }
                }
            }
        }
        
        protected override void HandleEnableEvent()
        {                        
            Activate();
        }

        protected override void HandleDisableEvent()
        {                                 
            Deactivate();
        }

        public override bool IsValid()
        {            
            return AsyncBanks.Any(s => s.IsValid()) || SyncBanks.Any(s => s.IsValid());
        }
        
        public void OnValidate()
        {
            if (AsyncMode)
                SyncBanks = new SoundBank[0];
        }
    }
}
