using System;
using System.Linq;
using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Switch Container", menuName = "AudioStudio/Sound/Switch Container")]
    public class SoundSwitchContainer : SoundContainer
    {
        #region Fields
        public AudioSwitchReference AudioSwitchReference = new AudioSwitchReference();		
        public SwitchEventMapping[] SwitchEventMappings = new SwitchEventMapping[0];
        public bool SwitchImmediately;
        public float CrossFadeTime = 0.5f;
        private Action<GameObject> _onSwitchChanged;
        #endregion

        internal override void Init()
        {
            base.Init();
            _onSwitchChanged = soundSource =>
            {
                Stop(soundSource, CrossFadeTime);
                Play(soundSource, CrossFadeTime);
            };
        }
        
        public override void OnValidate()
        {
            ChildEvents = SwitchEventMappings.Select(mapping => mapping.AudioEvent as SoundContainer).ToList();
            base.OnValidate();
        }

        internal override SoundContainer GetChildByPlayLogic(GameObject soundSource)
        {
            var audioSwitch = AsAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
            if (audioSwitch)
            {
                var asi = audioSwitch.GetOrAddSwitchInstance(soundSource);
                if (SwitchImmediately)
                {
                    if (asi.OnSwitchChanged == null || !asi.OnSwitchChanged.GetInvocationList().Contains(_onSwitchChanged))
                        asi.OnSwitchChanged += _onSwitchChanged;
                }

                foreach (var assignment in SwitchEventMappings)
                {
                    if (assignment.SwitchName == asi.CurrentSwitch)
                        return (SoundContainer) assignment.AudioEvent;
                }
            }
            return (SoundContainer) SwitchEventMappings[0].AudioEvent;
        }
    }
}