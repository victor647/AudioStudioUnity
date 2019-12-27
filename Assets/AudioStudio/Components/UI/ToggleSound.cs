using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/ToggleSound")]
    [DisallowMultipleComponent]
    public class ToggleSound : AsComponent
    {
        public PostEventReference[] ToggleOnEvents = new PostEventReference[0];
        public PostEventReference[] ToggleOffEvents = new PostEventReference[0];

        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(delegate
                {
                    PostEvents(toggle.isOn ? ToggleOnEvents : ToggleOffEvents, AudioTriggerSource.ToggleSound, gameObject);
                });
            }
        }

        public override bool IsValid()
        {            
            return ToggleOnEvents.Any(s => s.IsValid()) || ToggleOffEvents.Any(s => s.IsValid());
        }
    }
}
