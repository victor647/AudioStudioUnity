using System;
using UnityEngine;
using System.Linq;
using AudioStudio.Components;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Music Switch Container", menuName = "AudioStudio/Music/Switch Container")]
    public class MusicSwitchContainer : MusicContainer
    {
        public AudioSwitchReference AudioSwitchReference = new AudioSwitchReference();		
        public SwitchEventMapping[] SwitchEventMappings = new SwitchEventMapping[0];
        public bool SwitchImmediately;
        public bool SwitchToSamePosition;
        public float CrossFadeTime = 0.5f;
        private Action<GameObject> _onSwitchChanged;

        internal override void Init()
        {
            base.Init();
            _onSwitchChanged = g => MusicTransport.Instance.OnSwitchChanged(SwitchImmediately, SwitchToSamePosition, CrossFadeTime);
        }

        public override void OnValidate()
        {
            ChildEvents = SwitchEventMappings.Select(mapping => mapping.AudioEvent as MusicContainer).ToList();
            base.OnValidate();
        }
        
        public override MusicContainer GetEvent()
        {
            var audioSwitch = AsAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
            if (audioSwitch)
            {
                var asi = audioSwitch.GetOrAddSwitchInstance(GlobalAudioEmitter.GameObject);
                if (SwitchImmediately)
                {
                    if (asi.OnSwitchChanged == null || !asi.OnSwitchChanged.GetInvocationList().Contains(_onSwitchChanged))
                        asi.OnSwitchChanged += _onSwitchChanged;
                }
                foreach (var assignment in SwitchEventMappings)
                {
                    if (assignment.SwitchName == asi.CurrentSwitch)
                        return (MusicContainer) assignment.AudioEvent;
                }
            }
            return (MusicContainer) SwitchEventMappings[0].AudioEvent;
        }
    }
}