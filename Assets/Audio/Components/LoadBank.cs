using System.Linq;
using AudioStudio.Configs;
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
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SoundBank, AudioAction.Activate, "OnEnable", gameObject.name);
            foreach (var bank in Banks)
            {
                bank.Load();
            }            
        }

        protected override void HandleDisableEvent()
        {
            if (!UnloadOnDisable) return;
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SoundBank, AudioAction.Deactivate, "OnDisable", gameObject.name);
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
