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
    public class EmitterSound : AudioEmitter3D
    {        
        public PostEventReference[] AudioEvents = new PostEventReference[0];
        public float InitialDelay;
        public float MinInterval = 5;
        public float MaxInterval = 10;
        public EventPlayMode PlayMode = EventPlayMode.SingleLoop;
        private bool _isPlaying;
        public bool PauseIfInvisible;

        protected override void HandleEnableEvent()
        {                                    
            if (PlayMode == EventPlayMode.SingleLoop)
                PostEvents3D(AudioEvents, AudioTriggerSource.EmitterSound);
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
                PostEvents3D(AudioEvents, AudioTriggerSource.EmitterSound);  
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
                evt.Cancel(gameObject, AudioTriggerSource.EmitterSound);
            }
        }
        
        
        private void OnBecameVisible()
        {
            if (!PauseIfInvisible) return;
            foreach (var evt in AudioEvents)
            {
                AudioManager.ResumeEvent(evt.Name, GetSoundSource(), 0.1f, AudioTriggerSource.EmitterSound);
            }
        }
        
        private void OnBecameInvisible()
        {
            if (!PauseIfInvisible) return;
            foreach (var evt in AudioEvents)
            {
                AudioManager.PauseEvent(evt.Name, GetSoundSource(), 0.1f, AudioTriggerSource.EmitterSound);
            }
        }
        
        public override bool IsValid()
        {
            return AudioEvents.Any(s => s.IsValid());
        }    
    }
}
