using System;
using UnityEngine;

namespace AudioStudio.Components
{
    public class GlobalAudioEmitter : MonoBehaviour
    {
        private static GlobalAudioEmitter _instance;
        public static Action AudioUpdate;

        public static GameObject GameObject
        {
            get
            {
                if (!_instance)
                    Init();
                return _instance.gameObject;
            }			
        }
        
        public static GameObject InstrumentRack;

        public static void Init()
        {
            var go = new GameObject("Global Audio Emitter");
            _instance = go.AddComponent<GlobalAudioEmitter>();
            DontDestroyOnLoad(go);
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
            AudioUpdate += MidiManager.Instance.Update;
        }

        public static void Remove(GameObject go)
        {
            if (_instance) Destroy(_instance.gameObject, 0.1f);
        }

        private void Update()
        {
            AudioUpdate?.Invoke();
        }
    }
}