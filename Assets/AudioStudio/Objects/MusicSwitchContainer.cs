using UnityEngine;
using System.Linq;
using AudioStudio.Components;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Music Switch Container", menuName = "AudioStudio/Music/Switch Container")]
    public class MusicSwitchContainer : MusicContainer
    {
        #region Fields
        public AudioSwitchReference AudioSwitchReference = new AudioSwitchReference();		
        public SwitchEventMapping[] SwitchEventMappings = new SwitchEventMapping[0];
        public bool SwitchImmediately;
        public bool SwitchToSamePosition;
        public float CrossFadeTime = 0.5f;
        #endregion
        
        #region Editor
        public override void OnValidate()
        {
            ChildEvents = SwitchEventMappings.Select(mapping => mapping.AudioEvent as MusicContainer).ToList();
            base.OnValidate();
        }
        #endregion

        #region Playback         
        public override MusicContainer GetEvent()
        {
            var audioSwitch = AsAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
            if (audioSwitch)
            {
                var asi = audioSwitch.GetOrAddSwitchInstance(GlobalAudioEmitter.GameObject);
                asi.OnSwitchChanged = OnSwitchChanged;
                foreach (var assignment in SwitchEventMappings)
                {
                    if (assignment.SwitchName == asi.CurrentSwitch)
                        return (MusicContainer) assignment.AudioEvent;
                }
            }
            return (MusicContainer) SwitchEventMappings[0].AudioEvent;
        }

        private void OnSwitchChanged(GameObject soundSource)
        {
            MusicTransport.Instance.OnSwitchChanged(SwitchImmediately, SwitchToSamePosition, CrossFadeTime);
        }
        #endregion
    }
}