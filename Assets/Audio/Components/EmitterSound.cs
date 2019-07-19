using System.Linq;
using UnityEngine;


namespace AudioStudio
{   
    [AddComponentMenu("AudioStudio/EmitterSound")]
    [DisallowMultipleComponent]
    public class EmitterSound : AudioOnOffHandler
    {        
        public AudioEventReference[] AudioEvents = new AudioEventReference[0];
        public float FadeInTime = 0.5f;  
        public float FadeOutTime = 0.5f;                 

        protected override void HandleEnableEvent()
        {                                    
            if (AudioEvents.Length > 0)
            {
                PlaySound();     
                AudioManager.DebugToProfiler(MessageType.Component, ObjectType.EmitterSound, AudioAction.Activate, "OnEnable", gameObject.name);
            }                        
        }

        protected override void HandleDisableEvent()
        {
            if (AudioEvents.Length > 0)
            {
                StopSound();
                AudioManager.DebugToProfiler(MessageType.Component, ObjectType.EmitterSound, AudioAction.Deactivate, "OnDisable", gameObject.name);
            }
            base.HandleDisableEvent();
        }        
        
        private void PlaySound()
        {
            foreach (var evt in AudioEvents)
            {
                evt.Post(gameObject, FadeInTime);                
            }
        }

        private void StopSound()
        {
            foreach (var evt in AudioEvents)
            {
                evt.Stop(gameObject, FadeInTime);
            }
        }               

        public override bool IsValid()
        {
            return AudioEvents.Any(s => s.IsValid());
        }    
    }
}
