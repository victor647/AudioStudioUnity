using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/SliderSound")]
    [DisallowMultipleComponent]
    public class SliderSound : AsComponent
    {        
        public PostEventReference DragEvent = new PostEventReference();
        public PostEventReference PressEvent = new PostEventReference();
        public AudioParameterReference ConnectedParameter = new AudioParameterReference();
        public float ValueScale = 1f; 

        private void Start()
        {
            var s = GetComponent<Slider>();
            if (s == null) return;
            s.onValueChanged.AddListener(x =>
            {
                ConnectedParameter.SetValue(s.value * ValueScale, gameObject, AudioTriggerSource.SliderSound);                    
                DragEvent.Post(gameObject, AudioTriggerSource.SliderSound);                        
            });
        }

        public override bool IsValid()
        {
            return ConnectedParameter.IsValid() || DragEvent.IsValid();
        }
    }   
}

