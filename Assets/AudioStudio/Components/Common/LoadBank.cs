using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace AudioStudio.Components
{    
    [AddComponentMenu("AudioStudio/LoadBank")]
    [DisallowMultipleComponent]
    public class LoadBank : AudioEmitter3D
    {
        public bool AsyncMode = true;
        public SoundBank[] SyncBanks = new SoundBank[0];
        [FormerlySerializedAs("Banks")] public LoadBankReference[] AsyncBanks = new LoadBankReference[0];

        public override void Activate(GameObject source = null)
        {
            if (AsyncMode)
            {
                foreach (var bank in AsyncBanks)
                {
                    bank.Load(source, AudioTriggerSource.LoadBank);
                }
            }
            else
            {
                foreach (var bank in SyncBanks)
                {
                    if (bank.IsValid())
                        BankManager.LoadBank(bank, gameObject);
                }
            }
        }

        public override void Deactivate(GameObject source = null)
        {
            if (AsyncMode)
            {
                foreach (var bank in AsyncBanks)
                {
                    if (bank.UnloadOnDisable)
                        bank.Unload(source, AudioTriggerSource.LoadBank);
                }
            }
            else
            {
                foreach (var bank in SyncBanks)
                {
                    if (bank.IsValid())
                        BankManager.UnloadBank(bank, gameObject);
                }
            }
        }
        
        protected override void HandleEnableEvent()
        {                        
            Activate(gameObject);
        }

        protected override void HandleDisableEvent()
        {                                 
            Deactivate(gameObject);
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
