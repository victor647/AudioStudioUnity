using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/SliderSound")]
    [DisallowMultipleComponent]
    public class SliderSound : AsComponent
    {        
        public AudioEventReference DragEvent = new AudioEventReference();
        public AudioParameterReference ConnectedParameter = new AudioParameterReference();
        public float ValueScale = 1f; 

        private void Start()
        {
            var s = GetComponent<Slider>();
            if (s != null)
            {
                s.onValueChanged.AddListener(x =>
                {
                    ConnectedParameter.SetValue(s.value * ValueScale, gameObject, AudioTriggerSource.SliderSound);                    
                    DragEvent.Post(gameObject, -1f, AudioTriggerSource.SliderSound);                        
                });
            }
        }

        public override bool IsValid()
        {
            return ConnectedParameter.IsValid() || DragEvent.IsValid();
        }
    }   
}

