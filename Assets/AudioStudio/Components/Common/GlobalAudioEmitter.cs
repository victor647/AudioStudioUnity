using System;
using System.Collections;
using AudioStudio.Midi;
using UnityEngine;

namespace AudioStudio.Components
{
    public class GlobalAudioEmitter : MonoBehaviour
    {
        private static Action _audioUpdate;
        public static GameObject GameObject;
        internal static GameObject InstrumentRack;
        internal static GlobalAudioEmitter Instance;

        private void Awake()
        {
            if (GameObject)
                DestroyImmediate(GameObject);

            GameObject = gameObject;
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        internal static void AddMicrophone()
        {
            var microphone = new GameObject("Microphone Input");
            microphone.transform.parent = GameObject.transform;
            microphone.AddComponent<MicrophoneInput>();
        }

        internal static void AddMidi()
        {
            InstrumentRack = new GameObject("Midi Input");
            InstrumentRack.transform.parent = GameObject.transform;
            _audioUpdate += MidiManager.Instance.Update;
        }

        private void LateUpdate()
        {
            _audioUpdate?.Invoke();
            ListenerManager.UpdateListenerPositions();
            EmitterManager.UpdateAudioInstances();
        }

        internal void SetAudioMixerParameter(string parameterName, float currentValue, float targetValue, float fadeTime)
        {
            if (fadeTime == 0f)
                AudioManager.AudioMixer.SetFloat(parameterName, targetValue);
            else
            {
                var slewRate = (targetValue - currentValue) / fadeTime;
                StartCoroutine(SlewAudioMixer(parameterName, currentValue, targetValue, slewRate));
            }
        }

        private IEnumerator SlewAudioMixer(string parameterName, float currentValue, float targetValue, float slewRate)
        {
            while (Mathf.Abs(currentValue - targetValue) > slewRate * 0.01f)
            {
                currentValue += slewRate * Time.fixedDeltaTime;                
                AudioManager.AudioMixer.SetFloat(parameterName, currentValue);
                yield return new WaitForFixedUpdate();
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "AudioListener.png", AudioPathSettings.Instance.GizmosIconScaling);
        }
    }
}