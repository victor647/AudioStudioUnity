using System.Linq;
using UnityEngine;

namespace AudioStudio
{   
    [AddComponentMenu("AudioStudio/ColliderSound")]
    [DisallowMultipleComponent]
    public class ColliderSound : AudioPhysicsHandler
    {        
        public AudioEventReference[] EnterEvents = new AudioEventReference[0];
        public AudioEventReference[] ExitEvents = new AudioEventReference[0];                
        public AudioParameterReference CollisionForceParameter = new AudioParameterReference();     
        public float ValueScale = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (!CompareAudioTag(other)) return;            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.ColliderSound, AudioAction.Activate, "OnTriggerEnter", gameObject.name, "Enter with " + other.gameObject.name);
            PostEvents(EnterEvents, GetEmitter(other.gameObject));         
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!CompareAudioTag(other)) return;            
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.ColliderSound, AudioAction.Deactivate, "OnTriggerExit", gameObject.name, "Exit from " + other.gameObject.name);
            PostEvents(ExitEvents, GetEmitter(other.gameObject));       
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;     
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.ColliderSound, AudioAction.Activate, "OnCollisionEnter", gameObject.name, "Collider with " + other.gameObject.name);
            CollisionForceParameter.SetValue(other.relativeVelocity.magnitude * ValueScale, GetEmitter(other.gameObject));             
            PostEvents(EnterEvents, GetEmitter(other.gameObject));           
        }
        
        private void OnCollisionExit(Collision other)
        {
            if (!CompareAudioTag(other.collider)) return;
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.ColliderSound, AudioAction.Deactivate, "OnCollisionExit", gameObject.name, "Exit from " + other.gameObject.name);              
            PostEvents(ExitEvents, GetEmitter(other.gameObject));           
        }

        public override bool IsValid()
        {            
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid() || CollisionForceParameter.IsValid());
        }
    }
}
