using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/Slider Sound")]
    [DisallowMultipleComponent]
    public class SliderSound : AsUIHandler
    {        
        public PostEventReference DragEvent = new PostEventReference();
        public PostEventReference PressEvent = new PostEventReference();
        public AudioParameterReference ConnectedParameter = new AudioParameterReference();
        public float ValueScale = 1f; 

        public override void AddListener()
        {
            var slider = GetComponent<Slider>();
            if (slider)
                slider.onValueChanged.AddListener(OnSliderChanged);
        }

        private void OnSliderChanged(float value)
        {
            ConnectedParameter.SetValue(value * ValueScale, gameObject, AudioTriggerSource.SliderSound);
            DragEvent.Post(gameObject, AudioTriggerSource.SliderSound);
        }

        public override bool IsValid()
        {
            return ConnectedParameter.IsValid() || DragEvent.IsValid();
        }
    }   
}

