using System.Linq;
using AudioStudio.Configs;
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
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.ButtonSound, AudioAction.Activate, "OnClick", gameObject.name);
            PostEvents(ClickEvents);
        }

        public void OnPointerEnter(PointerEventData data)
        {            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.ButtonSound, AudioAction.Activate, "OnPointerEnter", gameObject.name);
            PointerEnterEvent.Post();
        }
        
        public void OnPointerExit(PointerEventData data)
        {            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.ButtonSound, AudioAction.Deactivate, "OnPointerExit", gameObject.name);
            PointerExitEvent.Post();
        }

        public override bool IsValid()
        {            
            return ClickEvents.Any(s => s.IsValid()) || PointerEnterEvent.IsValid() || PointerExitEvent.IsValid();
        }
    }
}

