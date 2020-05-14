using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/ButtonSound")]
    [DisallowMultipleComponent]    
    public class ButtonSound : AsUIHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public PostEventReference[] ClickEvents = new PostEventReference[0];
        public PostEventReference PointerEnterEvent = new PostEventReference();
        public PostEventReference PointerExitEvent = new PostEventReference();

        public void OnPointerClick(PointerEventData data)
        {
            PostEvents(ClickEvents, AudioTriggerSource.ButtonSound, gameObject); 
        }

        public void OnPointerEnter(PointerEventData data)
        {
            PointerEnterEvent.Post(gameObject, AudioTriggerSource.ButtonSound);
        }
        
        public void OnPointerExit(PointerEventData data)
        {
            PointerExitEvent.Post(gameObject, AudioTriggerSource.ButtonSound);
        }

        public override bool IsValid()
        {            
            return ClickEvents.Any(s => s.IsValid()) || PointerEnterEvent.IsValid() || PointerExitEvent.IsValid();
        }
    }
}

