using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio.Components
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
                s.onValueChanged.AddListener(x => ScrollEvent.Post(gameObject, -1f, AudioTriggerSource.ScrollSound));
        }

        public override bool IsValid()
        {
            return ScrollEvent.IsValid();
        }
    }   
}

