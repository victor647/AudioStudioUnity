using UnityEngine;

namespace AudioStudio
{
    [AddComponentMenu("AudioStudio/AnimationSound")]
    [DisallowMultipleComponent]
    public class AnimationSound : AsComponent
    {
        private AnimationAudioState _animationAudioState;

        public void SetAnimationState(AnimationAudioState newState)
        {
            _animationAudioState = newState;
        }

        public void PlaySound(string eventName)
        {            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.AnimationSound, AudioAction.Activate,"KeyFrame PlaySound", gameObject.name);            
            AudioManager.PlaySound(eventName, gameObject);
        }
        
        public void PlayVoice(string eventName)
        {            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.AnimationSound, AudioAction.Activate,"KeyFrame PlayVoice", gameObject.name);            
            AudioManager.PlayVoice(eventName);
        }

        public void StopSound(string eventName)
        {                                    
            AudioManager.StopSound(eventName, gameObject);
        }
        
        public void PlaySoundByState(string eventName)
        {			
            var eventSplit = eventName.Split('.');
            if (eventSplit.Length < 2)
            {
                PlaySound(eventName);
                return;
            }

            for (var i = 1; i < eventSplit.Length; i++)
            {
                if (_animationAudioState.ToString() == eventSplit[i]) PlaySound(eventSplit[0]);								
            }			
        }
        
        public void PlayVoiceByLanguage(string eventName)
        {			
            var eventSplit = eventName.Split('.');
            if (eventSplit.Length < 2)
            {
                PlayVoice(eventName);
                return;
            }

            for (var i = 1; i < eventSplit.Length; i++)
            {
                if (AudioManager.VoiceLanguage.ToString() == eventSplit[i]) PlayVoice(eventSplit[0]);								
            }			
        }

        public override bool IsValid()
        {
            return GetComponent<Animator>() != null;
        }
    }
}