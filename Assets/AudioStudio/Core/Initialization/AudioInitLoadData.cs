using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
    /// <summary>
    /// Data config for all the banks that should be loaded when game starts.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioInitLoadData", menuName = "AudioStudio/Audio Init Load Data")]
    public class AudioInitLoadData : ScriptableObject
    {
        public bool LoadBanks = true;
        public SoundBankReference[] Banks = new SoundBankReference[0];
        public bool PostEvents;
        public AudioEventReference[] AudioEvents = new AudioEventReference[0];

        internal void LoadAudioData()
        { 
            if (LoadBanks)
            {
                foreach (var bank in Banks)
                {
                    bank.Load(null, null, AudioTriggerSource.Initialization);
                }
            }

            if (PostEvents)
            {
                foreach (var evt in AudioEvents)
                {
                    evt.Play(null, AudioTriggerSource.Initialization);
                }
            }
        }
    }
}