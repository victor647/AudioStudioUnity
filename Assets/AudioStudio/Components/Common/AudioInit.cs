using UnityEngine;

namespace AudioStudio.Components
{
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
				AudioInitSettings = AudioInitSettings.Instance;
			
			if (InitOnAwake)
				AudioInitSettings.Initialize(LoadAudioData);
		}
	}
}