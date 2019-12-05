using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{    
    [AddComponentMenu("AudioStudio/LoadBank")]
    [DisallowMultipleComponent]
    public class LoadBank : AudioOnOffHandler
    {
        public SoundBankReference[] Banks = new SoundBankReference[0];
        public bool UnloadOnDisable = true;

        protected override void HandleEnableEvent()
        {
            foreach (var bank in Banks)
            {
                bank.Load(AudioTriggerSource.LoadBank);
            }            
        }

        protected override void HandleDisableEvent()
        {
            if (!UnloadOnDisable) return;
            foreach (var bank in Banks)
            {
                bank.Unload(AudioTriggerSource.LoadBank);
            }            
        }
        public override bool IsValid()
        {            
            return Banks.Any(s => s.IsValid());
        }
    }
}
