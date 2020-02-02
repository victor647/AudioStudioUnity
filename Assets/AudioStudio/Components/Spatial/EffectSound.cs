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
                PostEvents3D(EnableEvents, AudioTriggerSource.EffectSound);           
        }
        
        protected override void HandleDisableEvent()
        {
            PostEvents3D(DisableEvents, AudioTriggerSource.EffectSound);           
        }

        private IEnumerator PlaySoundDelayed()
        {
            yield return new WaitForSeconds(DelayTime);
            PostEvents3D(EnableEvents, AudioTriggerSource.EffectSound);           
        }

        public override bool IsValid()
        {
            return EnableEvents.Any(s => s.IsValid());
        }
    }
}
