using System;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{   
    public enum SoundPlayLogic
    {
        Layer, 
        Random,		
        Switch,        
        SequenceStep
    }

    [CreateAssetMenu(fileName = "New Sound Container", menuName = "AudioStudio/Sound/Container")]
    public class SoundContainer : AudioEvent
    {
        #region Fields    
        public SoundPlayLogic PlayLogic;                
        public List<SoundContainer> ChildEvents = new List<SoundContainer>();
        public SoundContainer ParentContainer;
        
        //Spatial Setting
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
        
        public bool RandomizeVolume;
        public float VolumeRandomRange = 0.1f;
        public bool RandomizePitch;
        public float PitchRandomRange = 0.1f;

        //voice limiting
        public bool EnableVoiceLimit;
        public bool OverrideVoicing;
        public VoiceLimiter VoiceLimiter;
        public byte VoiceLimitGlobal = 8;
        public byte VoiceLimitGameObject = 2;
        public byte Priority = 127;
        private readonly Dictionary<GameObject, byte> _voiceCount = new Dictionary<GameObject, byte>();                
        #endregion
        
        #region Editor
        public override void OnValidate()
        {               
            if (this is SoundClip) return;
            if (PlayLogic != SoundPlayLogic.Switch)
            {
                foreach (var child in ChildEvents)
                {
                    if (child) CopySettings(child);
                }
            }
            else
            {
                foreach (var assignment in SwitchEventMappings)
                {
                    var sc = assignment.AudioEvent as SoundContainer;
                    if (sc) CopySettings(sc);
                }
            }
        }

        private void CopySettings(SoundContainer child)
        {
            child.ParentContainer = this;
            child.IndependentEvent = false;

            if (!child.OverrideSpatial)
            {
                child.Is3D = Is3D;
                child.RollOffMode = RollOffMode;
                child.SpreadWidth = SpreadWidth;
                child.SpatialBlend = SpatialBlend;
                child.MinDistance = MinDistance;
                child.MaxDistance = MaxDistance;
                child.AttenuationCurve = AttenuationCurve;
            }

            child.EnableVoiceLimit = EnableVoiceLimit;
            if (!child.OverrideVoicing)
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
                
                child.Volume = Volume;
                child.RandomizeVolume = RandomizeVolume;
                child.VolumeRandomRange = VolumeRandomRange;
                child.Pitch = Pitch;
                child.RandomizePitch = RandomizePitch;
                child.PitchRandomRange = PitchRandomRange;
                child.CrossFadeTime = CrossFadeTime;

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

            if (this is SoundClip) return;
            
            if (PlayLogic != SoundPlayLogic.Switch)
            {
                SwitchEventMappings = null;
                if (ChildEvents.Any(c => !c)) 
                    Debug.LogError("ChildEvent of SoundContainer " + name + " is missing!");
            }
            else if (SwitchEventMappings.Any(c => !c.AudioEvent))
            {
                Debug.LogError("ChildEvent of SoundContainer " + name + " is missing!");
            }                        
            
            foreach (var evt in ChildEvents)
            {
                if (evt) evt.CleanUp();                    
            }            
        }
        
        public override bool IsValid()
        {
            return PlayLogic != SoundPlayLogic.Switch ? ChildEvents.Any(c => c != null) : SwitchEventMappings.Any(m => m.AudioEvent != null);
        }
        #endregion

        #region Initialize
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
        public override void Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
        {
            if (EnableVoiceLimit)
            {
                AddVoicing(soundSource);
                endCallback += RemoveVoicing;
            }
            var chosenClip = GetChild(soundSource, fadeInTime, endCallback);                                    
            if (chosenClip)                            
                chosenClip.Play(soundSource, fadeInTime, endCallback);                                            
        }

        public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Stop(soundSource, fadeOutTime);
            }
        }

        public virtual void StopAll(float fadeOutTime)
        {
            foreach (var evt in ChildEvents)
            {
                evt.StopAll(fadeOutTime);
            }
        }

        public override void Mute(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Mute(soundSource, fadeOutTime);
            }
        }

        public override void UnMute(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.UnMute(soundSource, fadeInTime);
            }
        }

        public override void Pause(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Pause(soundSource, fadeOutTime);
            }
        }

        public override void Resume(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var evt in ChildEvents)
            {                
                evt.Resume(soundSource, fadeInTime);
            }
        }

        private void OnSwitchChanged(GameObject soundSource)
        {
            Stop(soundSource, CrossFadeTime);
            Play(soundSource, CrossFadeTime);
        }

        private SoundClip GetChild(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {
            if (PlayLogic == SoundPlayLogic.Layer)
            {
                for (var i = 0; i < ChildEvents.Count; i++)
                {
                    var evt = ChildEvents[i];
                    evt.Init();
                    if (i == 0) //only the first clip will have the end callback
                        evt.Play(soundSource, fadeInTime, endCallback);
                    else
                        evt.Play(soundSource, fadeInTime);
                }

                return null;
            }
            var soundContainer = GetChildByPlayLogic(soundSource);                
            var clip = soundContainer as SoundClip;
            return clip ? clip : soundContainer.GetChild(soundSource, fadeInTime, endCallback);                       
        }

        private SoundContainer GetChildByPlayLogic(GameObject soundSource)
        {
            switch (PlayLogic)
            {
                case SoundPlayLogic.Random:
                    return GetRandomContainer();
                case SoundPlayLogic.Switch:
                    return GetSwitchContainer(soundSource);
                case SoundPlayLogic.SequenceStep:
                    return GetSequenceContainer();
            }
            return null;
        }

        public SoundContainer GetRandomContainer()
        {
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
        }

        private SoundContainer GetSwitchContainer(GameObject soundSource)
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

        private SoundContainer GetSequenceContainer()
        {
            LastSelectedIndex++;
            if (LastSelectedIndex == ChildEvents.Count) LastSelectedIndex = 0;
            return ChildEvents[LastSelectedIndex];
        }
        #endregion
        
        #region VoiceManagement        
        public void AddVoicing(GameObject soundSource)
        {
            if (_voiceCount.ContainsKey(soundSource))
                _voiceCount[soundSource]++;
            else
                _voiceCount[soundSource] = 1;
        }

        public void RemoveVoicing(GameObject soundSource)
        {
            if (_voiceCount.ContainsKey(soundSource))
            {
                _voiceCount[soundSource]--;
                if (_voiceCount[soundSource] == 0)
                    _voiceCount.Remove(soundSource);
            }                     
        }

        public void ClearVoicing(GameObject soundSource)
        {
            if (_voiceCount.ContainsKey(soundSource))
            {
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

        internal bool ReachVoiceLimit(GameObject soundSource, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {
            var voiceGlobal = CurrentVoicesGlobal() + 1;
            var voiceGameObject = CurrentVoicesGameObject(soundSource) + 1;
            
            if (voiceGlobal > VoiceLimitGlobal)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.VoiceLimit, trigger, name, soundSource,
                    "Global voice limit of " + VoiceLimitGlobal + " reaches");
                return true;
            }

            if (voiceGameObject > VoiceLimitGameObject)
            {
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.VoiceLimit, trigger, name, soundSource,
                    "GameObject voice limit of " + VoiceLimitGameObject + " reaches");
                return true;
            }
            return false;
        }
        #endregion
    }
}