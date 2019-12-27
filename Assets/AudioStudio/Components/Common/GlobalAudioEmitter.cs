using System;
using AudioStudio.Midi;
using UnityEngine;

namespace AudioStudio.Components
{
    public class GlobalAudioEmitter : MonoBehaviour
    {
        private static Action _audioUpdate;

        public static GameObject GameObject;

        public static GameObject InstrumentRack;

        private void Awake()
        {
            if (GameObject)
                DestroyImmediate(GameObject);

            GameObject = gameObject;
            DontDestroyOnLoad(gameObject);
        }

        public static void AddMicrophone()
        {
            var microphone = new GameObject("Microphone Input");
            microphone.transform.parent = GameObject.transform;
            microphone.AddComponent<MicrophoneInput>();
        }
        
        public static void AddMidi()
        {
            InstrumentRack = new GameObject("Midi Input");
            InstrumentRack.transform.parent = GameObject.transform;
            _audioUpdate += MidiManager.Instance.Update;
        }

        private void LateUpdate()
        {
            _audioUpdate?.Invoke();
            ListenerManager.UpdateListenerPositions();
        }
    }
}