using System;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{
    public abstract class SoundContainer : AudioEvent
    {
        #region Fields    
        public List<SoundContainer> ChildEvents = new List<SoundContainer>();
        public SoundContainer ParentContainer;
        [Range(0, 100)]

        // spatial Setting
        public bool OverrideSpatial;
        public bool Is3D;
        public float MinDistance = 1f;
        public float MaxDistance = 50f;
        [Range(0f, 1f)]
        public float SpatialBlend = 1f;
        [Range(0, 359)]
        public float SpreadWidth;
        public AudioRolloffMode RollOffMode = AudioRolloffMode.Logarithmic;
        public AnimationCurve AttenuationCurve;

        public string Name => ParentContainer ? ParentContainer.Name : name;
        // audio controls
        public bool RandomizeVolume;
        public float VolumeRandomRange = 0.1f;
        public bool RandomizePitch;
        public float PitchRandomRange = 0.1f;
        [Range(1, 100)]
        public byte Probability = 100;

        // voice management
        public bool EnableVoiceLimit;
        public VoiceLimiter VoiceLimiter;
        public byte VoiceLimitGlobal = 8;
        public byte VoiceLimitGameObject = 2;
        public byte Priority = 127;
        private readonly Dictionary<GameObject, byte> _voiceCount = new Dictionary<GameObject, byte>();                
        #endregion
        
        #region Editor
        public override void OnValidate()
        {
            for (var i = 0; i < ChildEvents.Count; i++)
            {
                if (ChildEvents[i] == this)
                    ChildEvents[i] = null;
                else if (ChildEvents[i])
                    CopySettings(ChildEvents[i]);
            }
        }

        private void CopySettings(SoundContainer child)
        {
            child.ParentContainer = this;
            child.IndependentEvent = false;

            if (!child.OverrideSpatial)
            {
                child.Is3D = Is3D;
                if (Is3D)
                {
                    child.RollOffMode = RollOffMode;
                    child.SpreadWidth = SpreadWidth;
                    child.SpatialBlend = SpatialBlend;
                    child.MinDistance = MinDistance;
                    child.MaxDistance = MaxDistance;
                    child.AttenuationCurve = AttenuationCurve;
                }
            }

            child.EnableVoiceLimit = EnableVoiceLimit;
            if (EnableVoiceLimit)
            {
                child.VoiceLimiter = VoiceLimiter;
                child.Priority = Priority;
                child.VoiceLimitGlobal = VoiceLimitGlobal;
                child.VoiceLimitGameObject = VoiceLimitGameObject;
            }

            if (!child.OverrideControls)
            {
                child.LowPassFilter = LowPassFilter;
                child.LowPassCutoff = LowPassCutoff;
                child.LowPassResonance = LowPassResonance;
                child.HighPassFilter = HighPassFilter;
                child.HighPassCutoff = HighPassCutoff;
                child.HighPassResonance = HighPassResonance;

                child.Probability = Probability;
                child.Volume = Volume;
                child.RandomizeVolume = RandomizeVolume;
                child.VolumeRandomRange = VolumeRandomRange;
                child.Pitch = Pitch;
                child.RandomizePitch = RandomizePitch;
                child.PitchRandomRange = PitchRandomRange;

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
                    Debug.LogError("Child Event of Sound Container " + name + " is missing!");
            }       
        }
        
        public override bool IsValid()
        {
            return ChildEvents.Any(c => c != null);
        }
        
        internal override AudioObjectType GetEventType()
        {
            return AudioObjectType.SFX;
        }

        public SoundContainer GetParentContainer()
        {
            return IndependentEvent || !ParentContainer ? this : ParentContainer.GetParentContainer();
        }
        #endregion

        #region Initialize
        internal override void Init()
        {
            foreach (var evt in ChildEvents)
            {
                if (evt)
                    evt.Init();
                else
                    Debug.LogError("ChildEvent of SoundContainer " + name + " is missing!");
            }
        }

        internal override void Dispose()
        {
            foreach (var evt in ChildEvents)
            {
                if (evt)
                    evt.Dispose();
            }
        }
        #endregion       

        #region Playback        

        public override string Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
        {
            // without a valid sound source or chance failed
            if (!soundSource || !WillPlayByProbability())
                return string.Empty;
            // check voice limit
            if (EnableVoiceLimit)
            {
                if (ReachVoiceLimit(soundSource)) 
                    return string.Empty;
                AddVoicing(soundSource);
                endCallback += RemoveVoicing;
            }
            var chosenClip = GetChild(soundSource, fadeInTime, endCallback);
            if (!chosenClip)
                return ChildEvents[0].name;
            chosenClip.Play(soundSource, fadeInTime, endCallback);
            return chosenClip.name;
        }

        protected bool WillPlayByProbability()
        {
            if (Probability < 100)
            {
                var roll = UnityEngine.Random.Range(0, 100);
                if (roll >= Probability)
                    return false;
            }
            return true;
        }

        public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Stop(soundSource, fadeOutTime);
            }
        }

        internal override void StopAll(float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {
                evt.StopAll(fadeOutTime);
            }
        }

        internal override void Mute(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Mute(soundSource, fadeOutTime);
            }
        }

        internal override void UnMute(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.UnMute(soundSource, fadeInTime);
            }
        }

        internal override void Pause(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Pause(soundSource, fadeOutTime);
            }
        }

        internal override void Resume(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Resume(soundSource, fadeInTime);
            }
        }

        protected virtual SoundClip GetChild(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {
            var soundContainer = GetChildByPlayLogic(soundSource);                
            var clip = soundContainer as SoundClip;
            return clip ? clip : soundContainer.GetChild(soundSource, fadeInTime, endCallback);                       
        }

        internal virtual SoundContainer GetChildByPlayLogic(GameObject soundSource)
        {
            return null;
        }
        #endregion
        
        #region VoiceManagement        
        protected void AddVoicing(GameObject soundSource)
        {
            if (_voiceCount.ContainsKey(soundSource))
                _voiceCount[soundSource]++;
            else
                _voiceCount[soundSource] = 1;
        }

        protected void RemoveVoicing(GameObject soundSource)
        {
            if (_voiceCount.ContainsKey(soundSource))
            {
                _voiceCount[soundSource]--;
                if (_voiceCount[soundSource] == 0)
                    _voiceCount.Remove(soundSource);
            }                     
        }

        private int CurrentVoicesGlobal()
        {            
            return _voiceCount.Aggregate(0, (total, count) => total + count.Value);
        }

        private int CurrentVoicesGameObject(GameObject soundSource)
        {
            if (!soundSource) soundSource = GlobalAudioEmitter.GameObject;
            if (_voiceCount.ContainsKey(soundSource)) 
                return _voiceCount[soundSource];
            _voiceCount[soundSource] = 0;
            return 0;
        }

        protected bool ReachVoiceLimit(GameObject soundSource)
        {
            if (!EnableVoiceLimit)
                return false;

            if (CurrentVoicesGlobal() >= VoiceLimitGlobal)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.VoiceLimit, AudioTriggerSource.Code, name, soundSource,
                    "Global voice limit of " + VoiceLimitGlobal + " reaches");
                return true;
            }

            if (CurrentVoicesGameObject(soundSource) >= VoiceLimitGameObject)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.VoiceLimit, AudioTriggerSource.Code, name, soundSource,
                    "GameObject voice limit of " + VoiceLimitGameObject + " reaches");
                return true;
            }
            return false;
        }
        #endregion
    }
}