using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/ButtonSound")]
    [DisallowMultipleComponent]    
    public class ButtonSound : AsComponent, IPointerEnterHandler, IPointerExitHandler
    {
        public AudioEventReference[] ClickEvents = new AudioEventReference[0];
        public AudioEventReference PointerEnterEvent = new AudioEventReference();
        public AudioEventReference PointerExitEvent = new AudioEventReference();

        private void Start()
        {
            var button = GetComponent<Button>();
            if (button == null || button.onClick == null) return;            
            button.onClick.AddListener(PlaySound);          
        }

        private void PlaySound()
        {
            PostEvents(ClickEvents, AudioTriggerSource.ButtonSound, gameObject);
        }

        public void OnPointerEnter(PointerEventData data)
        {
            PointerEnterEvent.Post(gameObject, -1f, AudioTriggerSource.ButtonSound);
        }
        
        public void OnPointerExit(PointerEventData data)
        {
            PointerExitEvent.Post(gameObject, -1f, AudioTriggerSource.ButtonSound);
        }

        public override bool IsValid()
        {            
            return ClickEvents.Any(s => s.IsValid()) || PointerEnterEvent.IsValid() || PointerExitEvent.IsValid();
        }
    }
}

