using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Tools;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
    public enum MusicPlayLogic
    {
        Layer, 
        Random,		
        Switch,
        SequenceContinuous,
        SequenceStep
    }

    [Serializable]
    public class TransitionExitData
    {
        public MusicTransitionReference Target = new MusicTransitionReference();
        public float FadeOutTime = 1f;
        public float ExitOffset;
        public TransitionInterval Interval = TransitionInterval.Immediate;
        public BarAndBeat GridLength;
    }
    
    [Serializable]
    public class TransitionEntryData
    {
        public MusicTransitionReference Source = new MusicTransitionReference();
        public float FadeInTime;
        public float EntryOffset = 1f;
        public MusicSegmentReference TransitionSegment = new MusicSegmentReference();
    }
    
    [CreateAssetMenu(fileName = "New Music Container", menuName = "AudioStudio/Music/Container")]
    public class MusicContainer : AudioEvent
    {
        #region Fields
        public MusicPlayLogic PlayLogic;
        public MusicContainer ParentContainer;
        public byte LoopCount;
        public List<MusicContainer> ChildEvents = new List<MusicContainer>();
        
        public bool OverrideTransition;
        public TransitionEntryData[] TransitionEntryConditions = new TransitionEntryData[1];
        public TransitionExitData[] TransitionExitConditions = new TransitionExitData[1];
        public bool SwitchToSamePosition;
        public bool LoopEntireSequence;

        #endregion
        
        #region Editor
        public override void OnValidate()
        {            
            if (this is MusicTrack) return;
            if (PlayLogic != MusicPlayLogic.Switch)
            {
                foreach (var evt in ChildEvents)
                {
                    if (evt) CopySettings(evt);
                }
            }
            else
            {
                foreach (var assignment in SwitchEventMappings)
                {
                    var mc = assignment.AudioEvent as MusicContainer;
                    if (mc) CopySettings(mc);
                }
            }
        }

        private void CopySettings(MusicContainer child)
        {
            child.ParentContainer = this;
            child.IndependentEvent = false;
            
            if (!child.OverrideTransition)
            {
                child.TransitionEntryConditions = TransitionEntryConditions;
                child.TransitionExitConditions = TransitionExitConditions;
            }

            if (!child.OverrideControls)
            {
                child.LowPassFilter = LowPassFilter;
                child.LowPassCutoff = LowPassCutoff;
                child.LowPassResonance = LowPassResonance;
                child.HighPassFilter = HighPassFilter;
                child.HighPassCutoff = HighPassCutoff;
                child.HighPassResonance = HighPassResonance;

                child.Volume = Volume;
                child.Pan = Pan;
                child.Pitch = Pitch;

                child.SubMixer = SubMixer;
                child.AudioMixer = AudioMixer;
                child.Mappings = Mappings;
            }

            child.OnValidate();
        }

        public override void CleanUp()
        {
            if (IndependentEvent) 
                ParentContainer = null;
            
            if (this is MusicTrack) return;
            
            if (PlayLogic != MusicPlayLogic.Switch)
            {
                SwitchEventMappings = new SwitchEventMapping[0];
                if (ChildEvents.Any(c => !c)) 
                    Debug.LogError("ChildEvent of MusicContainer " + name + " is missing!");
            }
            else 
            {
                ChildEvents.Clear();
                if (SwitchEventMappings.Any(c => !c.AudioEvent))
                    Debug.LogError("ChildEvent of MusicContainer " + name + " is missing!");
            }
            foreach (var evt in ChildEvents)
            {
                if (evt) evt.CleanUp();                    
            }
        }

        public override bool IsValid()
        {
            return PlayLogic != MusicPlayLogic.Switch ? ChildEvents.Any(c => c != null) : SwitchEventMappings.Any(m => m.AudioEvent != null);
        }

        internal override AudioObjectType GetEventType()
        {
            return AudioObjectType.Music;
        }

        public MusicContainer GetParentContainer()
        {
            return IndependentEvent || !ParentContainer ? this : ParentContainer.GetParentContainer();
        }
        #endregion

        #region Initialization

        internal override void Init()
        {
            LastSelectedIndex = 255;
            if (PlayLogic != MusicPlayLogic.Switch)
            {
                foreach (var evt in ChildEvents)
                {
                    evt.Init();
                }
            }
            else
            {
                foreach (var mapping in SwitchEventMappings)
                {
                    mapping.AudioEvent.Init();
                } 
            }          
        }

        internal override void Dispose()
        {            
            if (PlayLogic != MusicPlayLogic.Switch)
            {
                foreach (var evt in ChildEvents)
                {
                    evt.Dispose();
                }
            }
            else
            {
                foreach (var mapping in SwitchEventMappings)
                {
                    mapping.AudioEvent.Dispose();
                } 
            }        
        }
        #endregion                
               
        #region Playback         

        public override string Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
        {               
            return MusicTransport.Instance.SetMusicQueue(this);
        }

        public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
        {
            MusicTransport.Instance.Stop(fadeOutTime);
        }

        internal override void StopAll(float fadeOutTime = 0f)
        {
            if (MusicTransport.Instance.CurrentEvent == this)
                MusicTransport.Instance.Stop(fadeOutTime);
        }

        internal override void Mute(GameObject soundSource, float fadeOutTime = 0f)
        {
            MusicTransport.Instance.Mute(fadeOutTime);
        }

        internal override void UnMute(GameObject soundSource, float fadeInTime = 0f)
        {
            MusicTransport.Instance.UnMute(fadeInTime);
        }

        internal override void Pause(GameObject soundSource, float fadeOutTime = 0f)
        {
            MusicTransport.Instance.Pause(fadeOutTime);
        }

        internal override void Resume(GameObject soundSource, float fadeInTime = 0f)
        {
            MusicTransport.Instance.Resume(fadeInTime);
        }

        public MusicContainer GetEvent()
        {
            switch (PlayLogic)
            {
                case MusicPlayLogic.Random:
                    if (ChildEvents.Count < 2)
                        return ChildEvents[0];
                    var selectedIndex = Random.Range(0, ChildEvents.Count);
                    if (!AvoidRepeat) 
                        return ChildEvents[selectedIndex];
                    while (selectedIndex == LastSelectedIndex)
                    {
                        selectedIndex = Random.Range(0, ChildEvents.Count);
                    }
                    LastSelectedIndex = (byte)selectedIndex;
                    return ChildEvents[selectedIndex];
                case MusicPlayLogic.Switch:
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
                case MusicPlayLogic.SequenceStep:
                    LastSelectedIndex++;
                    if (LastSelectedIndex == ChildEvents.Count) 
                        LastSelectedIndex = 0;
                    return ChildEvents[LastSelectedIndex];
                case MusicPlayLogic.SequenceContinuous:
                    LastSelectedIndex = 0;
                    return ChildEvents[0];                    
            }
            return null;
        }

        public MusicContainer GetNextEvent()
        {
            LastSelectedIndex++;
            if (LastSelectedIndex == ChildEvents.Count)
            {
                LastSelectedIndex = 0;
                return LoopEntireSequence ? ChildEvents[0] : null;
            }
            return ChildEvents[LastSelectedIndex];
        }
        
        private void OnSwitchChanged(GameObject soundSource)
        {
            MusicTransport.Instance.OnSwitchChanged(SwitchImmediately, SwitchToSamePosition, CrossFadeTime);
        }
        #endregion
    }
}