using System.Linq;
using UnityEngine;

namespace AudioStudio
{    
    [AddComponentMenu("AudioStudio/LoadBank")]
    [DisallowMultipleComponent]
    public class LoadBank : AudioOnOffHandler
    {
        public SoundBankReference[] Banks = new SoundBankReference[0];
        public bool UnloadOnDisable = true;

        protected override void HandleEnableEvent()
        {   
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.SoundBank, AudioAction.Activate, "OnEnable", gameObject.name);
            foreach (var bank in Banks)
            {
                bank.Load();
            }            
        }

        protected override void HandleDisableEvent()
        {
            if (!UnloadOnDisable) return;
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.SoundBank, AudioAction.Deactivate, "OnDisable", gameObject.name);
            foreach (var bank in Banks)
            {
                bank.Unload();
            }            
        }
        public override bool IsValid()
        {            
            return Banks.Any(s => s.IsValid());
        }
    }
}
