using System;
using UnityEngine;

namespace AudioStudio
{
    public class GlobalAudioEmitter : MonoBehaviour
    {
        private static GlobalAudioEmitter _instance;

        public static GameObject GameObject
        {
            get
            {
                if (_instance) return _instance.gameObject;				
                var go = new GameObject("Global Audio Emitter");
                _instance = go.AddComponent<GlobalAudioEmitter>();
                if (Application.isPlaying) 
                    DontDestroyOnLoad(go);
                return go;
            }			
        }

        public static void Init()
        {
            var go = new GameObject("Global Audio Emitter");
            _instance = go.AddComponent<GlobalAudioEmitter>();
            DontDestroyOnLoad(go);
        }
        
        public static void Remove(GameObject go)
        {
            if (_instance) Destroy(_instance.gameObject, 0.1f);
        }

        private void Update()
        {
            MidiManager.Instance.Update();
        }
    }
}