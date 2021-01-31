using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/Scroll Sound")]    
    [DisallowMultipleComponent]
    public class ScrollSound : AsUIHandler
    {        
        public PostEventReference ScrollEvent = new PostEventReference();

        public override void AddListener()
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

