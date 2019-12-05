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
    
    [CreateAssetMenu(fileName = "New Music Container", menuName = "Audio/Music/Container")]
    public class MusicContainer : AudioEvent
    {
        #region Fields
        public MusicPlayLogic PlayLogic;
        
        public byte LoopCount;
        public List<MusicContainer> ChildEvents = new List<MusicContainer>();
        
        public bool OverrideTransition;
        public TransitionInterval TransitionInterval = TransitionInterval.NextBar;
        public BarAndBeat GridLength;
        
        public float DefaultEntryOffset;        
        public float DefaultExitOffset = -1f;
        public bool SwitchToSamePosition;

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
            child.IndependentEvent = false;
            if (!child.OverrideTransition)
            {
                child.TransitionInterval = TransitionInterval;
                child.GridLength = GridLength;
                child.DefaultFadeInTime = DefaultFadeInTime;
                child.DefaultEntryOffset = DefaultEntryOffset;
                child.DefaultFadeOutTime = DefaultFadeOutTime;
                child.DefaultExitOffset = DefaultExitOffset;
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
            if (Platform == Platform.Web)
                TransitionInterval = TransitionInterval.Immediate;
            
            if (this is MusicTrack) return;
            
            if (PlayLogic != MusicPlayLogic.Switch)
            {
                SwitchEventMappings = null;
                if (ChildEvents.Any(c => !c)) 
                    Debug.LogError("ChildEvent of MusicContainer " + name + " is missing!");
            }
            else if (SwitchEventMappings.Any(c => !c.AudioEvent))
            {
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

        #endregion

        #region Initialization
        public override void Init()
        {
            LastSelectedIndex = 255;
            foreach (var evt in ChildEvents) evt.Init();            
        }
        
        public override void Dispose()
        {            
            foreach (var evt in ChildEvents) evt.Dispose();
        }
        #endregion                
               
        #region Playback                
        public override void PostEvent(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            if (fadeInTime < 0f) fadeInTime = DefaultFadeInTime;
            Play(soundSource, fadeInTime, endCallback);
        }

        public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {               
            MusicTransport.Instance.SetMusicQueue(this, fadeInTime, DefaultFadeOutTime, DefaultExitOffset, DefaultEntryOffset);
        }
        
        public override void Stop(GameObject soundSource, float fadeOutTime)
        {     
            if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;
            MusicTransport.Instance.Stop(fadeOutTime);
        }

        public MusicContainer GetEvent()
        {
            switch (PlayLogic)
            {
                case MusicPlayLogic.Random:
                    if (ChildEvents.Count < 2)
                    {
                        AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Music, AudioAction.PostEvent, AudioTriggerSource.Code, name, MusicTransport.GameObject, "Random MusicEvent only has 1 element");
                        return ChildEvents[0];
                    }
                    var selectedIndex = Random.Range(0, ChildEvents.Count);
                    if (!AvoidRepeat) return ChildEvents[selectedIndex];
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
            return LastSelectedIndex == ChildEvents.Count ? null : ChildEvents[LastSelectedIndex];
        }
        
        private void OnSwitchChanged(GameObject soundSource)
        {
            MusicTransport.Instance.OnSwitchChanged(SwitchImmediately, SwitchToSamePosition, CrossFadeTime);
        }
        #endregion
    }
}