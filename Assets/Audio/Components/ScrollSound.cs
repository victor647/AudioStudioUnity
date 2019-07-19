using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio
{
    [AddComponentMenu("AudioStudio/ScrollSound")]    
    [DisallowMultipleComponent]
    public class ScrollSound : AsComponent
    {        
        public AudioEventReference ScrollEvent = new AudioEventReference();

        private void Start()
        {
            var s = GetComponent<ScrollRect>();
            if (s != null)
            {
                s.onValueChanged.AddListener(x =>
                {                    
                    PlaySound();                    
                });
            }
        }

        private void PlaySound()
        {            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.ScrollSound, AudioAction.Activate, "OnScroll", gameObject.name);            
            ScrollEvent.Post();       
        }

        public override bool IsValid()
        {
            return ScrollEvent.IsValid();
        }
    }   
}

