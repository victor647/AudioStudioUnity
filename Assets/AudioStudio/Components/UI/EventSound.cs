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
        public PostEventReference AudioEvent = new PostEventReference();
        public EventTriggerType TriggerType;
    }
    
    [AddComponentMenu("AudioStudio/EventSound")]
    public class EventSound : AsComponent
    {
        public UIAudioEvent[] UIAudioEvents = new UIAudioEvent[0];

        private void Start()
        {
            var et = AsUnityHelper.GetOrAddComponent<EventTrigger>(gameObject);
            foreach (var evt in UIAudioEvents)
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