using System;
using System.Linq;
using UnityEngine;

namespace AudioStudio
{
    public class MicrophoneInput : MonoBehaviour
    {
        public static MicrophoneInput Instance;
        public float FFTBands = 1024;
        
        private AudioSource _source;
        private float[] _frequencyBands = new float[512];
        private float[] _sampleBuffer;

        private void Awake()
        {
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.ignoreListenerVolume = true;
        }

        public void StartVoiceRecord()
        {
            _source.clip = Microphone.Start(null, true, 1, 44100);
            _source.Play();
        }

        public int GetHighestFrequencyBand()
        {
            var maxBand = 0;
            var maxAmp = 0f;
            for (var i = 0; i < _frequencyBands.Length; i++)
            {
                if (_frequencyBands[i] > maxAmp)
                {
                    maxAmp = _frequencyBands[i];
                    maxBand = i;
                }
            }
            return maxBand;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            _sampleBuffer = data;
        }

        public float GetAmplitude()
        {
            var totalAmp = _sampleBuffer.Sum(sample => Mathf.Pow(sample, 2));
            return Mathf.Sqrt(totalAmp / _sampleBuffer.Length);
        }
        
        private void Update()
        {
            _source.GetSpectrumData(_frequencyBands, 0, FFTWindow.Blackman);
        }

        public void EndVoiceRecord()
        {
            _source.Stop();
            Microphone.End(null);
        }
    }
}