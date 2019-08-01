using System;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
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

    public enum EmitterGameObject
    {
        Self,
        Child,
        Independent
    }
    
    [CreateAssetMenu(fileName = "New Sound Container", menuName = "Audio/Sound/Container")]
    public class SoundContainer : AudioEvent
    {
        #region Fields    
        public SoundPlayLogic PlayLogic;                
        public List<SoundContainer> ChildEvents = new List<SoundContainer>();
        public SoundContainer ParentContainer;
        
        //Spatial Settings
        public SpatialSetting SpatialSetting;
        public bool IsUpdatePosition;
        public EmitterGameObject EmitterGameObject = EmitterGameObject.Child;
        
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
            child.EmitterGameObject = EmitterGameObject;

            if (!child.OverrideSpatial)
            {
                child.IsUpdatePosition = IsUpdatePosition;
                
                child.SpatialSetting = SpatialSetting;
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
                child.DefaultFadeInTime = DefaultFadeInTime;
                child.DefaultFadeOutTime = DefaultFadeOutTime;
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
            
            if (!IsUpdatePosition) 
                SpatialSetting = null;
            
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
        public override void PostEvent(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {                        
            if (fadeInTime < 0) fadeInTime = DefaultFadeInTime;            
            if (EnableVoiceLimit && ReachVoiceLimit(soundSource)) return;

            //Determine which game object to play the sound from
            switch (EmitterGameObject)
            {
                case EmitterGameObject.Independent:
                    var tempSoundSource = new GameObject(name);				
                    tempSoundSource.transform.position = soundSource.transform.position;								
                    Play(tempSoundSource, fadeInTime, endCallback);
                    break;
                case EmitterGameObject.Self:
                case EmitterGameObject.Child:
                    Play(soundSource, fadeInTime, endCallback);  
                    break;
            }
        }
        
        public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
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

        public override void Stop(GameObject soundSource, float fadeOutTime)
        {
            if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;                        

            foreach (var evt in ChildEvents)
            {                
                evt.Stop(soundSource, fadeOutTime);
            }
        }

        public virtual void StopAll(float fadeOutTime)
        {
            if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;   
            
            foreach (var evt in ChildEvents)
            {
                evt.StopAll(fadeOutTime);
            }
            if (IndependentEvent) 
                AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.StopEvent, name, "Global");
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
            var audioSwitch = AudioAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
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
                
        private bool ReachVoiceLimit(GameObject soundSource)
        {
            var voiceGlobal = CurrentVoicesGlobal() + 1;
            var voiceGameObject = CurrentVoicesGameObject(soundSource) + 1;
            
            if (voiceGlobal > VoiceLimitGlobal)
            {
                AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.VoiceLimit, name, "Global",
                    "Global voice limit of " + VoiceLimitGlobal + " reaches");
                return true;
            }

            if (voiceGameObject > VoiceLimitGameObject)
            {
                AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.VoiceLimit, name, soundSource.name,
                    "GameObject voice limit of " + VoiceLimitGameObject + " reaches");
                return true;
            }
                             
            AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.PostEvent, name, soundSource.name,
                "Voices: " + voiceGlobal + " global, " + voiceGameObject + " on game object");
            return false;
        }
        #endregion
    }
}