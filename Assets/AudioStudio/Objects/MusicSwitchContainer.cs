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
            foreach (var assignment in SwitchEventMappings)
            {
                var mc = assignment.AudioEvent as MusicContainer;
                if (mc) CopySettings(mc);
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
                    Debug.LogError("Child Event of Music Switch Container " + name + " is missing!");              
            }
        }

        public override bool IsValid()
        {
            return SwitchEventMappings.Any(m => m.AudioEvent != null);
        }
        #endregion

        #region Initialization

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