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

        public void PlaySound(AnimationEvent evt)
        {
            var eventName = evt.stringParameter;
            var eventSplit = eventName.Split('.');
            if (eventSplit.Length < 2)
            {
                if (evt.intParameter == 0 || evt.intParameter - 1 == (int) _animationAudioState)
                    DoPlaySound(eventName);
            }
            else
            {
                for (var i = 1; i < eventSplit.Length; i++)
                {
                    if (_animationAudioState.ToString() == eventSplit[i] || evt.animatorStateInfo.IsName(eventSplit[i]))
                        DoPlaySound(eventSplit[0]);								
                } 
            }
        }
        
        public void PlayVoice(AnimationEvent evt)
        {
            var eventName = evt.stringParameter;
            var eventSplit = eventName.Split('.');
            if (eventSplit.Length < 2)
            {
                if (evt.intParameter == 0 || evt.intParameter - 1 == (int) AudioManager.VoiceLanguage)
                    DoPlayVoice(eventName);
            }
            else
            {
                for (var i = 1; i < eventSplit.Length; i++)
                {
                    if (_animationAudioState.ToString() == eventSplit[i] || evt.animatorStateInfo.IsName(eventSplit[i]))
                        DoPlayVoice(eventSplit[0]);								
                } 
            }
        }
        
        private void DoPlaySound(string eventName)
        {
            AudioManager.PlaySound(eventName, GetSoundSource(), 0f, null, AudioTriggerSource.AnimationSound);
        }
        
        private void DoPlayVoice(string eventName)
        {
            AudioManager.PlayVoice(eventName, gameObject, 0f, null, AudioTriggerSource.AnimationSound);
        }

        public void PlayMusic(string eventName)
        {
            AudioManager.PlayMusic(eventName, 0f, gameObject, AudioTriggerSource.AnimationSound);
        }

        public void StopSound(string eventName)
        {                                    
            AudioManager.StopSound(eventName, GetSoundSource(), 0.2f, AudioTriggerSource.AnimationSound);
        }
        
        public void StopVoice(string eventName)
        {                                    
            AudioManager.StopVoice(eventName, gameObject, 0.2f, AudioTriggerSource.AnimationSound);
        }

        public void StopMusic(string eventName)
        {                                    
            AudioManager.StopMusic(0.5f, gameObject, AudioTriggerSource.AnimationSound);
        }

        public override bool IsValid()
        {
            return GetComponent<Animator>() != null || GetComponent<Animation>() != null;
        }
    }
}