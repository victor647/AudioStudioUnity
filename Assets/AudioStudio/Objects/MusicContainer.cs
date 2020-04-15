using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Tools;

namespace AudioStudio.Configs
{
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
    
    public abstract class MusicContainer : AudioEvent
    {
        #region Fields
        public MusicContainer ParentContainer;
        public byte LoopCount;
        public List<MusicContainer> ChildEvents = new List<MusicContainer>();
        
        public bool OverrideTransition;
        public TransitionEntryData[] TransitionEntryConditions = new TransitionEntryData[1];
        public TransitionExitData[] TransitionExitConditions = new TransitionExitData[1];
        #endregion
        
        #region Editor
        public override void OnValidate()
        {            
            foreach (var evt in ChildEvents)
            {
                if (evt) CopySettings(evt);
            }
        }

        protected void CopySettings(MusicContainer child)
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
            
            foreach (var evt in ChildEvents)
            {
                if (evt) 
                    evt.CleanUp();
                else
                    Debug.LogError("Child Event of Music Container " + name + " is missing!");
            }
        }

        public override bool IsValid()
        {
            return ChildEvents.Any(c => c != null);
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
            foreach (var evt in ChildEvents)
            {
                evt.Init();
            }    
        }

        internal override void Dispose()
        {            
            foreach (var evt in ChildEvents)
            {
                evt.Dispose();
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

        public virtual MusicContainer GetEvent()
        {
            return null;
        }
        #endregion
    }
}