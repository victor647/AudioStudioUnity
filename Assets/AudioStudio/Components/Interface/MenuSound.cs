using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/Menu Sound")]
    [DisallowMultipleComponent]
    public class MenuSound : AsUIHandler
    {
        public PostEventReference[] OpenEvents = new PostEventReference[0];
        public PostEventReference[] CloseEvents = new PostEventReference[0];
        
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
