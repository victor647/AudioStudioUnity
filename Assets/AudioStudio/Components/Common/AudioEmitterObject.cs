using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioEmitterObject : AudioPhysicsHandler
    {
        public bool IsUpdatePosition = true;
        public bool StopOnDestroy = true;

        private GameObject _emitter;

        public GameObject GetSoundSource()
        {
            if (IsUpdatePosition)
            {
                if (!StopOnDestroy)
                {
                    _emitter = new GameObject(gameObject.name + " (AudioSource)");
                    _emitter.transform.position = transform.position;
                }
                else
                    _emitter = gameObject;
            }
            else
                _emitter = GlobalAudioEmitter.GameObject;
            return _emitter;
        }
    }
}
