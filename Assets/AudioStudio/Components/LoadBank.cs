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
        public PostEventReference[] LoadFinishEvents = new PostEventReference[0];

        protected override void HandleEnableEvent()
        {
            foreach (var bank in Banks)
            {
                bank.Load(PostLoadFinishEvents, gameObject, AudioTriggerSource.LoadBank);
            }            
        }

        protected override void HandleDisableEvent()
        {
            if (!UnloadOnDisable) return;
            foreach (var bank in Banks)
            {
                bank.Unload(gameObject, AudioTriggerSource.LoadBank);
            }            
        }

        private void PostLoadFinishEvents()
        {
            if (this != null && gameObject != null && LoadFinishEvents != null)
                PostEvents(LoadFinishEvents, AudioTriggerSource.LoadBank, gameObject);
        }
        
        public override bool IsValid()
        {            
            return Banks.Any(s => s.IsValid());
        }
    }
}
