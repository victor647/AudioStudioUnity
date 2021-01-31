using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/Dropdown Sound")]
    [DisallowMultipleComponent]
    public class DropdownSound : AsUIHandler
    {
        public PostEventReference[] ValueChangeEvents = new PostEventReference[0];
        public PostEventReference[] PopupEvents = new PostEventReference[0];
        public PostEventReference[] CloseEvents = new PostEventReference[0];
        
        private bool _isPoppedUp;

        public override void AddListener()
        {
            var dropDown = gameObject.GetComponent<Dropdown>();
            if (dropDown == null) return;
            dropDown.onValueChanged.AddListener(x => PostEvents(ValueChangeEvents, AudioTriggerSource.DropdownSound, gameObject));

            var trigger = AsUnityHelper.GetOrAddComponent<EventTrigger>(gameObject);
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
            _isPoppedUp = true;
            PostEvents(PopupEvents, AudioTriggerSource.DropdownSound, gameObject);
        }

        private void PlayCloseSound()
        {
            if (_isPoppedUp)
                PostEvents(CloseEvents, AudioTriggerSource.DropdownSound, gameObject);
            _isPoppedUp = false;
        }

        public override bool IsValid()
        {
            return PopupEvents.Any(s => s.IsValid()) || ValueChangeEvents.Any(s => s.IsValid()) || CloseEvents.Any(s => s.IsValid());
        }
    }
}

