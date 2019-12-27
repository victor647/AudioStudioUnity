using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/AnimationSound")]
    [DisallowMultipleComponent]
    public class AnimationSound : AudioEmitterObject
    {
        private AnimationAudioState _animationAudioState;

        public void SetAnimationState(AnimationAudioState newState)
        {
            if (_animationAudioState == newState) return;
            _animationAudioState = newState;
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.AudioState, AudioAction.SetValue, AudioTriggerSource.AudioState, newState.ToString(), gameObject);
        }

        public void PlaySound(string eventName)
        {
            AudioManager.PlaySound(eventName, GetSoundSource(), 0f, null, AudioTriggerSource.AnimationSound);
        }
        
        public void PlayVoice(string eventName)
        {
            AudioManager.PlayVoice(eventName, gameObject, 0f, null, AudioTriggerSource.AnimationSound);
        }

        public void StopSound(string eventName)
        {                                    
            AudioManager.StopSound(eventName, GetSoundSource());
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
            return GetComponent<Animator>() != null || GetComponent<Animation>() != null;
        }
    }
}