using UnityEngine;
using UnityEngine.UI;

namespace AudioStudio
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
                    AudioManager.DebugToProfiler(MessageType.Component, ObjectType.SliderSound, AudioAction.Activate, "OnDrag", gameObject.name);
                    ConnectedParameter.SetValue(s.value * ValueScale);                    
                    DragEvent.Post();                  
                });
            }
        }

        public override bool IsValid()
        {
            return ConnectedParameter.IsValid() || DragEvent.IsValid();
        }
    }   
}

