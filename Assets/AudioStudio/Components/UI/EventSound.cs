using System;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.EventSystems;


namespace AudioStudio.Components
{
    [Serializable]
    public class UIAudioEvent
    {
        public EventTriggerType TriggerType;
        public PostEventReference AudioEvent = new PostEventReference();
		
        public override bool Equals(object obj)
        {
            if (obj is UIAudioEvent other)
                return AudioEvent.Equals(other.AudioEvent) && TriggerType == other.TriggerType;
            return false;
        }
    }
    
    [AddComponentMenu("AudioStudio/EventSound")]
    public class EventSound : AsComponent
    {
        public UIAudioEvent[] AudioEvents = new UIAudioEvent[0];

        private void Start()
        {
            var et = AsUnityHelper.GetOrAddComponent<EventTrigger>(gameObject);
            foreach (var evt in AudioEvents)
            {
                var trigger = new EventTrigger.Entry
                {
                    eventID = evt.TriggerType,
                };
                et.triggers.Add(trigger);
                trigger.callback.AddListener(data => evt.AudioEvent.Post(gameObject, AudioTriggerSource.EventSound));
            }
        }
    }
}