using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace AudioStudio.Components
{
    public class AudioLanguageSettings : MonoBehaviour
    {        
        public Toggle[] LanguageToggles;  

        private void Start()
        {
            if (LanguageToggles.Length > 0)
            {
                for (var i = 0; i < LanguageToggles.Length; i++)
                {
                    var index = i;
                    LanguageToggles[i].onValueChanged.AddListener((b) =>
                    {
                        if (b) AudioManager.VoiceLanguage = (Languages)index;
                    });
                }
            }
        }
		
        private void OnDisable()
        {					
            PlayerPrefs.Save();
        }
    }
}