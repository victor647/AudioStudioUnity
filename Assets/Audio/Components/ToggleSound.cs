using System.Linq;
using AudioStudio.Configs;
using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/ToggleSound")]
    [DisallowMultipleComponent]
    public class ToggleSound : AsComponent
    {
        public AudioEventReference[] ToggleOnEvents = new AudioEventReference[0];
        public AudioEventReference[] ToggleOffEvents = new AudioEventReference[0];

        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(delegate
                {
                    PlayToggleSound(toggle);
                });
            }
        }

        private void PlayToggleSound(Toggle toggle)
        {            
            if(toggle.isOn)
            {
                AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.ToggleSound, AudioAction.Activate, "ToggleOn", gameObject.name);
                foreach (var evt in ToggleOnEvents)
                {
                    evt.Post();
                }                
                toggle.interactable = false;
                return;
            }            
            toggle.interactable = true;                    
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.ToggleSound, AudioAction.Deactivate, "ToggleOff", gameObject.name);            
            foreach (var evt in ToggleOffEvents)
            {
                evt.Post();
            }            
        }

        public override bool IsValid()
        {            
            return ToggleOnEvents.Any(s => s.IsValid()) || ToggleOffEvents.Any(s => s.IsValid());
        }
    }
}
