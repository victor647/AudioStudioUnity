using System;
using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio.Components
{
    public class AudioTransformFollower : MonoBehaviour
    {
        public AudioEmitterObject Master;
        public int VoiceCount = 1;

        private void Start()
        {
            Invoke(nameof(CheckVoiceExistence), 0.5f);
        }

        public void AudioEnd(AudioEventInstance voice)
        {
            VoiceCount--;
            if (VoiceCount < 1)
                Destroy(gameObject);
            else
                Destroy(voice);
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
    }
}