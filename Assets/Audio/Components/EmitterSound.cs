using System.Collections;
using System.Linq;
using AudioStudio.Configs;
using UnityEngine;


namespace AudioStudio.Components
{   
    public enum EventPlayMode
    {
        SingleLoop,
        PeriodTrigger
    }
    
    [AddComponentMenu("AudioStudio/EmitterSound")]
    [DisallowMultipleComponent]
    public class EmitterSound : AudioOnOffHandler
    {        
        public AudioEventReference[] AudioEvents = new AudioEventReference[0];
        public float FadeInTime = 0.5f;  
        public float FadeOutTime = 0.5f;
        public float InitialDelay;
        public float MinInterval = 5;
        public float MaxInterval = 10;
        public EventPlayMode PlayMode = EventPlayMode.SingleLoop;
        private bool _isPlaying;

        protected override void HandleEnableEvent()
        {                                    
            if (PlayMode == EventPlayMode.SingleLoop)
                PlaySoundSingle();
            else
                StartCoroutine(PlaySoundPeriod());                      
        }

        protected override void HandleDisableEvent()
        {
            if (AudioEvents.Length > 0)
            {
                StopSound();
                AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.EmitterSound, AudioAction.Deactivate, "OnDisable", gameObject.name);
            }
            base.HandleDisableEvent();
        }        
        
        private void PlaySoundSingle()
        {
            foreach (var audioEvent in AudioEvents)
            {
                audioEvent.Post(gameObject);
            }  
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.EmitterSound, AudioAction.Activate, "OnEnable", gameObject.name);
        }
        
        private IEnumerator PlaySoundPeriod()
        {
            if (_isPlaying) yield break;
            _isPlaying = true;
            yield return new WaitForSeconds(InitialDelay);			
            while (isActiveAndEnabled)
            {
                foreach (var audioEvent in AudioEvents)
                {
                    audioEvent.Post(gameObject, FadeInTime);
                }
                var waitSecond = Random.Range(MinInterval, MaxInterval);
                AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.EmitterSound, AudioAction.Activate, "OnPeriod", gameObject.name, 
                    "Next sound plays in " + waitSecond + "s");
                yield return new WaitForSeconds(waitSecond);
            }
            _isPlaying = false;
        }
        
        private void StopSound()
        {
            foreach (var evt in AudioEvents)
            {
                evt.Stop(gameObject, FadeOutTime);
            }
        }               

        public override bool IsValid()
        {
            return AudioEvents.Any(s => s.IsValid());
        }    
    }
}
