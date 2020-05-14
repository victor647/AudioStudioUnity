using UnityEngine;

namespace AudioStudio.Components
{
    /// <summary>
    /// Should be added to the game start scene for referencing AudioInitSettings config.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioInit : MonoBehaviour
    {
        public AudioInitSettings AudioInitSettings;
        public bool InitOnAwake = true;
        public bool LoadAudioData = true; 

        private void Awake()
        {
            if (AudioInitSettings)			
                AudioInitSettings.Instance = AudioInitSettings;									
            else		
                // without a referenced config, create a new one instead
                AudioInitSettings = AudioInitSettings.Instance;
			
            if (InitOnAwake)
                AudioInitSettings.Initialize(LoadAudioData);
        }
    }
}