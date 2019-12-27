using System.Collections;
using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
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
    public class EmitterSound : AudioEmitterObject
    {        
        public PostEventReference[] AudioEvents = new PostEventReference[0];
        public float InitialDelay;
        public float MinInterval = 5;
        public float MaxInterval = 10;
        public EventPlayMode PlayMode = EventPlayMode.SingleLoop;
        private bool _isPlaying;

        protected override void HandleEnableEvent()
        {                                    
            if (PlayMode == EventPlayMode.SingleLoop)
                PostEvents(AudioEvents, AudioTriggerSource.EmitterSound, GetSoundSource());
            else
                StartCoroutine(PlaySoundPeriod());                      
        }

        private IEnumerator PlaySoundPeriod()
        {
            if (_isPlaying) yield break;
            _isPlaying = true;
            yield return new WaitForSeconds(InitialDelay);			
            while (isActiveAndEnabled)
            {
                PostEvents(AudioEvents, AudioTriggerSource.EmitterSound, GetSoundSource());  
                var waitSecond = Random.Range(MinInterval, MaxInterval);
                yield return new WaitForSeconds(waitSecond);
            }
            _isPlaying = false;
        }

        protected override void HandleDisableEvent()
        {
            if (!StopOnDestroy) return;
            foreach (var evt in AudioEvents)
            {
                evt.Stop(gameObject, AudioTriggerSource.EmitterSound);
            }
        }
        
        public override bool IsValid()
        {
            return AudioEvents.Any(s => s.IsValid());
        }    
    }
}
