using System.Collections.Generic;
using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioEmitterObject : AudioPhysicsHandler
    {
        public bool IsUpdatePosition = true;
        public bool StopOnDestroy = true;

        private GameObject _emitter;

        public GameObject GetSoundSource(GameObject emitter = null)
        {
            if (IsUpdatePosition)
            {
                if (!emitter)
                    emitter = gameObject;
                if (StopOnDestroy)
                    _emitter = gameObject;
                else if (!_emitter)
                {
                    _emitter = new GameObject(emitter.name + " (AudioSource)");
                    _emitter.transform.position = emitter.transform.position;
                }
            }
            else
                _emitter = GlobalAudioEmitter.GameObject;
            return _emitter;
        }
    }
}
