using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/MenuSound")]
    [DisallowMultipleComponent]
    public class MenuSound : AudioOnOffHandler
    {
        public AudioEventReference[] OpenEvents = new AudioEventReference[0];
        public AudioEventReference[] CloseEvents = new AudioEventReference[0];
        
        protected override void HandleEnableEvent()
        {            
            PostEvents(OpenEvents, AudioTriggerSource.MenuSound, gameObject);                                
        }

        protected override void HandleDisableEvent()
        {               
            PostEvents(CloseEvents, AudioTriggerSource.MenuSound, gameObject); 
        }
        
        public override bool IsValid()
        {            
            return OpenEvents.Any(s => s.IsValid()) || CloseEvents.Any(s => s.IsValid());
        }
    }
}
