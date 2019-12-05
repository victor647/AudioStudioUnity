using System.Linq;
using UnityEngine;

namespace AudioStudio.Components
{
    public class MicrophoneInput : MonoBehaviour
    {
        public static MicrophoneInput Instance;
        public int FFTBands = 512;
        
        private AudioSource _source;
        private float[] _frequencyBands;
        private float[] _sampleBuffer;
        
        public static bool IsRecording;
        public float Amplitude { get; private set; }

        private void Awake()
        {
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _frequencyBands = new float[FFTBands];
        }

        public void StartRecording()
        {
            IsRecording = true;
            if (Microphone.devices.Length == 0) return;
            _source.clip = Microphone.Start(null, true, 1, 44100);
            while (Microphone.GetPosition(null) <= 0)
            {
            }
            _source.outputAudioMixerGroup = AudioManager.GetAudioMixer("Microphone");
            _source.Play();
        }
        
        public void EndRecording()
        {
            if (_source)
                _source.Stop();
            Microphone.End(null);
            IsRecording = false;
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

        public float[] GetFrequencyBands(int bandCount)
        {
            var resultBands = new float[bandCount];
            var fftPerBand = FFTBands / bandCount;
            for (var i = 0; i < bandCount; i ++)
            {
                var totalAmp = 0f;
                for (var j = 0; j < fftPerBand; j++)
                {
                    totalAmp += _frequencyBands[i * fftPerBand + j];
                }
                resultBands[i] = totalAmp;
            }
            return resultBands;
        }
        
        public float[] GetEqualPowerBands(int bandCount)
        {
            var resultBands = new float[bandCount];
            var bandWidths = new float[bandCount];
            for (var i = 0; i < bandCount; i++)
            {
                bandWidths[i] = Mathf.Pow(2, i * 10f / bandCount);
            }
            var fftPerBand = FFTBands / bandWidths.Sum();
            if (fftPerBand < 0) return null;
            
            var currentBand = 0;
            for (var i = 0; i < bandCount; i ++)
            {
                var bandWidthInt = Mathf.RoundToInt(fftPerBand * bandWidths[i]);
                var totalAmp = 0f;
                for (var j = 0; j < bandWidthInt; j++)
                {
                    totalAmp += _frequencyBands[currentBand + j];
                }
                currentBand += bandWidthInt;
                resultBands[i] = totalAmp / fftPerBand;
            }
            return resultBands;
        }

        private float GetAmplitude()
        {
            var totalAmp = _sampleBuffer.Sum(sample => Mathf.Pow(sample, 2));
            var amp = Mathf.Sqrt(totalAmp / _sampleBuffer.Length);
            return amp;
        }
        
        private void Update()
        {
            if (_source)
                _source.GetSpectrumData(_frequencyBands, 0, FFTWindow.Blackman);
        }
        
        private void OnAudioFilterRead(float[] data, int channels)
        {
            _sampleBuffer = data;
            Amplitude = GetAmplitude();
        }
    }
}