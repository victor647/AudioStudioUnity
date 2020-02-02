using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioEmitterObject : AudioPhysicsHandler
    {
        public bool IsUpdatePosition = true;
        public bool StopOnDestroy = true;

        private GameObject _emitter;

        public GameObject GetSoundSource(bool isPlay = false)
        {
            if (_emitter)
            {
                if (IsUpdatePosition && !StopOnDestroy && isPlay)
                {
                    var slave = _emitter.GetComponent<AudioTransformFollower>();
                    if (slave)
                        slave.VoiceCount++;
                }
                return _emitter;       
            }

            if (IsUpdatePosition)
            {
                if (!StopOnDestroy && isPlay)
                {
                    _emitter = new GameObject(gameObject.name + " (AudioSource)");
                    var slave = _emitter.AddComponent<AudioTransformFollower>();
                    slave.Master = this;
                }
                else
                    _emitter = gameObject;
            }
            else
                _emitter = StopOnDestroy ? gameObject : GlobalAudioEmitter.GameObject;
            return _emitter;
        }
        
        protected void PostEvents3D(IEnumerable<PostEventReference> events, AudioTriggerSource trigger)
        {
            foreach (var evt in events)
            {
                evt.Post(GetSoundSource(evt.Action == AudioEventAction.Play), trigger);
            }  
        }
    }
}
