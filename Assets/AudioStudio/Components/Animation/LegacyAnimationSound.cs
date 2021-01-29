using System;
using System.Linq;
using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio.Components
{
    [Serializable]
    public class AnimationAudioEvent
    {
        public string ClipName;
        public int Frame;
        public PostEventReference AudioEvent = new PostEventReference();
		
        public override bool Equals(object obj)
        {
            if (obj is AnimationAudioEvent other)
                return AudioEvent.Equals(other.AudioEvent) && ClipName == other.ClipName && Frame == other.Frame;
            return false;
        }
        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
    [AddComponentMenu("AudioStudio/LegacyAnimationSound")]
    [DisallowMultipleComponent]
    public class LegacyAnimationSound : AnimationSound
    {
        public int FrameRate = 30;
        public AnimationAudioEvent[] AudioEvents = new AnimationAudioEvent[0];

        protected override void Start()
        {
            base.Start();
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            var anim = GetComponent<Animation>();
            if (!anim) return;
            
            foreach (var evt in AudioEvents)
            {
                var clip = anim.GetClip(evt.ClipName);
                if (!clip) continue;
                var existingEvent = clip.events.FirstOrDefault(e => e.stringParameter == evt.AudioEvent.Name);
                if (existingEvent != null) continue;
                var newEvent = new AnimationEvent {time = evt.Frame * 1f / FrameRate, stringParameter = evt.AudioEvent.Name};
                switch (evt.AudioEvent.Action)
                {
                    case AudioEventAction.Play:
                        switch (evt.AudioEvent.Type)
                        {
                            case AudioEventType.SFX:
                                newEvent.functionName = "PlaySound";
                                break;
                            case AudioEventType.Voice:
                                newEvent.functionName = "PlayVoice";
                                break;
                            case AudioEventType.Music:
                                newEvent.functionName = "PlayMusic";
                                break;
                        }
                        break;
                    case AudioEventAction.Stop:
                        switch (evt.AudioEvent.Type)
                        {
                            case AudioEventType.SFX:
                                newEvent.functionName = "StopSound";
                                break;
                            case AudioEventType.Voice:
                                newEvent.functionName = "StopVoice";
                                break;
                            case AudioEventType.Music:
                                newEvent.functionName = "StopMusic";
                                break;
                        }
                        break;
                    default:
                        continue;
                }
                clip.AddEvent(newEvent);
            }
        }

        public override bool IsValid()
        {
            return AudioEvents.Any(e => e.AudioEvent.IsValid());
        }
    }
}