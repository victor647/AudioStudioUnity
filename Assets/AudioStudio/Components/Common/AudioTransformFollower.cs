using System;
using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioTransformFollower : MonoBehaviour
    {
        public AudioEmitterObject Master;

        private void Start()
        {
            Invoke(nameof(CheckVoiceExistence), 0.5f);
        }

        private void CheckVoiceExistence()
        {
            if (!GetComponent<AudioSource>())
                Destroy(gameObject);
        }
        
        private void LateUpdate()
        {
            if (!Master) return;
            transform.position = Master.transform.position;
        }

        private void OnDestroy()
        {
            Master.EmitterInstantiated = false;
        }
    }
}