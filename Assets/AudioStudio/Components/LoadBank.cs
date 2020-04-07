using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{    
    [AddComponentMenu("AudioStudio/LoadBank")]
    [DisallowMultipleComponent]
    public class LoadBank : AudioEmitterObject
    {
        public bool AsyncMode = true;
        public SoundBank Bank;
        public LoadBankReference[] Banks = new LoadBankReference[0];

        public override void Activate(GameObject source = null)
        {
            if (AsyncMode)
            {
                foreach (var bank in Banks)
                {
                    bank.Load(source, AudioTriggerSource.LoadBank);
                }
            }
            else if (Bank.IsValid())
                AsAssetLoader.DoLoadBank(Bank);
        }

        public override void Deactivate(GameObject source = null)
        {
            if (AsyncMode)
            {
                foreach (var bank in Banks)
                {
                    if (bank.UnloadOnDisable)
                        bank.Unload(source, AudioTriggerSource.LoadBank);
                }
            }
            else if (Bank.IsValid())
                AsAssetLoader.UnloadBank(Bank);
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
            return Banks.Any(s => s.IsValid()) || Bank.IsValid();
        }
    }
}
