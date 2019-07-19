using System.Linq;
using UnityEngine;


namespace AudioStudio
{
    [AddComponentMenu("AudioStudio/MenuSound")]
    [DisallowMultipleComponent]
    public class MenuSound : AudioOnOffHandler
    {
        public AudioEventReference[] OpenEvents = new AudioEventReference[0];
        public AudioEventReference[] CloseEvents = new AudioEventReference[0];
        
        protected override void HandleEnableEvent()
        {            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.MenuSound, AudioAction.Activate, "Open", gameObject.name);
            foreach (var evt in OpenEvents)
            {
                evt.Post();
            }                   
        }

        protected override void HandleDisableEvent()
        {               
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.MenuSound, AudioAction.Deactivate, "Close", gameObject.name);
            foreach (var evt in CloseEvents)
            {
                evt.Post();
            }    
        }
        
        public override bool IsValid()
        {            
            return OpenEvents.Any(s => s.IsValid()) || CloseEvents.Any(s => s.IsValid());
        }
    }
}
