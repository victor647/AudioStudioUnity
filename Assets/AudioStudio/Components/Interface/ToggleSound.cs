using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/Toggle Sound")]
    [DisallowMultipleComponent]
    public class ToggleSound : AsUIHandler
    {
        public PostEventReference[] ToggleOnEvents = new PostEventReference[0];
        public PostEventReference[] ToggleOffEvents = new PostEventReference[0];

        public override void AddListener()
        {
            var toggle = GetComponent<Toggle>();
            if (toggle)
                toggle.onValueChanged.AddListener(PlaySound);
        }

        private void PlaySound(bool isOn)
        {
            PostEvents(isOn ? ToggleOnEvents : ToggleOffEvents, AudioTriggerSource.ToggleSound, gameObject);
        }

        public override bool IsValid()
        {            
            return ToggleOnEvents.Any(s => s.IsValid()) || ToggleOffEvents.Any(s => s.IsValid());
        }
    }
}
