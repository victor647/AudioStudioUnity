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
        public PostEventReference ScrollEvent = new PostEventReference();

        private void Start()
        {
            var s = GetComponent<ScrollRect>();
            if (s != null)
                s.onValueChanged.AddListener(x => ScrollEvent.Post(gameObject, AudioTriggerSource.ScrollSound));
        }

        public override bool IsValid()
        {
            return ScrollEvent.IsValid();
        }
    }   
}

