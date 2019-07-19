using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace AudioStudio
{
    [AddComponentMenu("AudioStudio/DropdownSound")]
    [DisallowMultipleComponent]
    public class DropdownSound : AsComponent
    {
        public AudioEventReference[] ValueChangeEvents = new AudioEventReference[0];
        public AudioEventReference[] PopupEvents = new AudioEventReference[0];
        public AudioEventReference[] CloseEvents = new AudioEventReference[0];
        
        private bool _isPoppedUp;
        private Dropdown _dropDown;

        private void Start()
        {
            _dropDown = gameObject.GetComponent<Dropdown>();
            if (_dropDown == null) return;
            _dropDown.onValueChanged.AddListener(x => PlayValueChangeSound());

            var trigger = _dropDown.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<EventTrigger>();
            }

            var entry = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};

            entry.callback.AddListener((x) => { PlayPopupSound(); });
            trigger.triggers.Add(entry);

            var submit = new EventTrigger.Entry {eventID = EventTriggerType.Submit};

            submit.callback.AddListener((x) => { PlayPopupSound(); });
            trigger.triggers.Add(submit);

            var close = new EventTrigger.Entry {eventID = EventTriggerType.Select};

            close.callback.AddListener((x) => { PlayCloseSound(); });
            trigger.triggers.Add(close);

            var cancel = new EventTrigger.Entry {eventID = EventTriggerType.Cancel};

            cancel.callback.AddListener((x) => { PlayCloseSound(); });
            trigger.triggers.Add(cancel);            
        }

        private void PlayPopupSound()
        {
            if (PopupEvents.Length > 0)
            {
                AudioManager.DebugToProfiler(MessageType.Component, ObjectType.DropdownSound, AudioAction.Activate, "Popup", gameObject.name);
                PostEvents(PopupEvents);
            }
            _isPoppedUp = true;
        }

        private void PlayValueChangeSound()
        {
            if (ValueChangeEvents.Length > 0)
            {
                AudioManager.DebugToProfiler(MessageType.Component, ObjectType.DropdownSound, AudioAction.Activate, "OnValueChange", gameObject.name);
                PostEvents(ValueChangeEvents);
            }            
        }

        private void PlayCloseSound()
        {
            if (_isPoppedUp)
            {
                if (CloseEvents.Length > 0)
                {
                    AudioManager.DebugToProfiler(MessageType.Component, ObjectType.DropdownSound, AudioAction.Deactivate, "Close", gameObject.name);
                    PostEvents(CloseEvents);
                }
            }            
            _isPoppedUp = false;
        }

        public override bool IsValid()
        {
            return PopupEvents.Any(s => s.IsValid()) || ValueChangeEvents.Any(s => s.IsValid()) || CloseEvents.Any(s => s.IsValid());
        }
    }
}

