using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{   
    [AddComponentMenu("AudioStudio/ColliderSound")]
    [DisallowMultipleComponent]
    public class ColliderSound : AudioEmitterObject
    {        
        public PostEventReference[] EnterEvents = new PostEventReference[0];
        public PostEventReference[] ExitEvents = new PostEventReference[0];                
        public AudioParameterReference CollisionForceParameter = new AudioParameterReference();     
        public float ValueScale = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (!CompareAudioTag(other)) return;
            PostEvents(EnterEvents, AudioTriggerSource.ColliderSound, GetEmitter(other.gameObject));         
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!CompareAudioTag(other)) return;
            PostEvents(ExitEvents, AudioTriggerSource.ColliderSound, GetEmitter(other.gameObject));       
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;
            CollisionForceParameter.SetValue(other.relativeVelocity.magnitude * ValueScale, GetEmitter(other.gameObject), AudioTriggerSource.ColliderSound);             
            PostEvents(EnterEvents, AudioTriggerSource.ColliderSound, GetEmitter(other.gameObject));           
        }
        
        private void OnCollisionExit(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;
            PostEvents(ExitEvents, AudioTriggerSource.ColliderSound, GetEmitter(other.gameObject));           
        }

        public override bool IsValid()
        {            
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid() || CollisionForceParameter.IsValid());
        }
    }
}
