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
        #endregion
        #region Editor
        public override void OnValidate()
        {               
            foreach (var assignment in SwitchEventMappings)
            {
                var sc = assignment.AudioEvent as SoundContainer;
                if (sc) CopySettings(sc);
            }
        }

        public override void CleanUp()
        {
            if (IndependentEvent) 
                ParentContainer = null;
            
            ChildEvents.Clear();
            foreach (var mapping in SwitchEventMappings)
            {
                if (mapping.AudioEvent)
                    mapping.AudioEvent.CleanUp();
                else
                    Debug.LogError("Child Event of Sound Container " + name + " is missing!");              
            }            
        }

        public override bool IsValid()
        {
            return SwitchEventMappings.Any(m => m.AudioEvent != null);
        }
        #endregion
        
        #region Initialize
        internal override void Init()
        {
            foreach (var mapping in SwitchEventMappings)
            {
                mapping.AudioEvent.Init();
            } 
        }

        internal override void Dispose()
        {
            foreach (var mapping in SwitchEventMappings)
            {
                mapping.AudioEvent.Dispose();
            }
        }
        #endregion    
        
        internal override SoundContainer GetChildByPlayLogic(GameObject soundSource)
        {
            var audioSwitch = AsAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
            if (audioSwitch)
            {
                var asi = audioSwitch.GetOrAddSwitchInstance(soundSource);
                if (SwitchImmediately)
                {
                    asi.OnSwitchChanged = OnSwitchChanged;
                }

                foreach (var assignment in SwitchEventMappings)
                {
                    if (assignment.SwitchName == asi.CurrentSwitch)
                        return (SoundContainer) assignment.AudioEvent;
                }
            }
            return (SoundContainer) SwitchEventMappings[0].AudioEvent;
        }
        
        private void OnSwitchChanged(GameObject soundSource)
        {
            Stop(soundSource, CrossFadeTime);
            Play(soundSource, CrossFadeTime);
        }
    }
}