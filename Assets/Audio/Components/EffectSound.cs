using System.Linq;
using UnityEngine;


namespace AudioStudio
{
    [AddComponentMenu("AudioStudio/EffectSound")]
    [DisallowMultipleComponent]
    public class EffectSound : AudioOnOffHandler
    {
        public AudioEventReference[] EnableEvents = new AudioEventReference[0];

        protected override void HandleEnableEvent()
        {
            base.HandleEnableEvent();            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.EffectSound, AudioAction.Activate, "OnEnable", gameObject.name);
            foreach (var evt in EnableEvents)
            {
                evt.Post(gameObject);
            }            
        }

        public override bool IsValid()
        {
            return EnableEvents.Any(s => s.IsValid());
        }
    }
}
