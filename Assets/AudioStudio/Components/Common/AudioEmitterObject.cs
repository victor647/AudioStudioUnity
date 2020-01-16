using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioEmitterObject : AudioPhysicsHandler
    {
        public bool IsUpdatePosition = true;
        public bool StopOnDestroy = true;

        private GameObject _emitter;
        internal bool EmitterInstantiated;

        public GameObject GetSoundSource()
        {
            if (_emitter) 
                return _emitter;
            
            if (IsUpdatePosition)
            {
                if (!StopOnDestroy && !EmitterInstantiated)
                {
                    _emitter = new GameObject(gameObject.name + " (AudioSource)");
                    var slave = _emitter.AddComponent<AudioTransformFollower>();
                    slave.Master = this;
                    EmitterInstantiated = true;
                }
                else
                    _emitter = gameObject;
            }
            else
                _emitter = StopOnDestroy ? gameObject : GlobalAudioEmitter.GameObject;
            return _emitter;
        }
    }
}
