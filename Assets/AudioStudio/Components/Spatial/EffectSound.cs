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
        public PostEventReference[] EnableEvents = new PostEventReference[0];
        public PostEventReference[] DisableEvents = new PostEventReference[0];
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
            PostEvents(DisableEvents, AudioTriggerSource.EffectSound, GetSoundSource());           
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
