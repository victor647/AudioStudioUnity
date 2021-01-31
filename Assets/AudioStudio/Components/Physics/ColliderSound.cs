using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{   
    [AddComponentMenu("AudioStudio/Collider Sound")]
    [DisallowMultipleComponent]
    public class ColliderSound : AudioEmitter3D
    {        
        public PostEventReference[] EnterEvents = new PostEventReference[0];
        public PostEventReference[] ExitEvents = new PostEventReference[0];                
        public AudioParameterReference CollisionForceParameter = new AudioParameterReference();     
        public float ValueScale = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (!CompareAudioTag(other)) return;
            PostEvents(EnterEvents, AudioTriggerSource.ColliderSound, GetEmitter);         
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!CompareAudioTag(other)) return;
            PostEvents(ExitEvents, AudioTriggerSource.ColliderSound, GetEmitter);       
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;
            CollisionForceParameter.SetValue(other.relativeVelocity.magnitude * ValueScale, GetEmitter, AudioTriggerSource.ColliderSound);             
            PostEvents(EnterEvents, AudioTriggerSource.ColliderSound, GetEmitter);           
        }
        
        private void OnCollisionExit(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;
            PostEvents(ExitEvents, AudioTriggerSource.ColliderSound, GetEmitter);           
        }

        public override bool IsValid()
        {            
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid() || CollisionForceParameter.IsValid());
        }
    }
}
