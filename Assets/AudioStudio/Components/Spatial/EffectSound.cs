using System.Collections;
using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;


namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/EffectSound")]
    [DisallowMultipleComponent]
    public class EffectSound : AudioEmitterObject
    {
        public AudioEventReference[] EnableEvents = new AudioEventReference[0];
        [Range(0f, 2f)]
        public float DelayTime;

        protected override void HandleEnableEvent()
        {
            base.HandleEnableEvent();
            if (DelayTime > 0f)
                StartCoroutine(PlaySoundDelayed());
            else
                PostEvents(EnableEvents, AudioTriggerSource.EffectSound, GetSoundSource());           
        }
        
        protected override void HandleDisableEvent()
        {
            if (IsUpdatePosition || !StopOnDestroy) return;
            foreach (var evt in EnableEvents)
            {
                evt.Stop(null, 0.2f, AudioTriggerSource.EffectSound);
            }
        }

        private IEnumerator PlaySoundDelayed()
        {
            yield return new WaitForSeconds(DelayTime);
            PostEvents(EnableEvents, AudioTriggerSource.EffectSound, GetSoundSource());           
        }

        public override bool IsValid()
        {
            return EnableEvents.Any(s => s.IsValid());
        }
    }
}
