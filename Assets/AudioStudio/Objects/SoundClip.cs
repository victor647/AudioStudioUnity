using System;
using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Clip", menuName = "AudioStudio/Sound/Clip")]
    public class SoundClip : SoundContainer
    {      
        #region Fields
        public AudioClip Clip;        
        public bool Loop;
        public bool SeekRandomPosition;
        #endregion
        
        #region Editor
        public override void CleanUp()
        {
            ChildEvents.Clear();
            if (!Clip)
                Debug.LogError("AudioClip of SoundClip " + name + " is missing!");
        }
        
        public override bool IsValid()
        {
            return Clip != null;
        }
        #endregion        
        
        #region Initialize

        internal override void Init()
        {            
            _playingInstances = new List<AudioEventInstance>();
            Clip.LoadAudioData();									                   
        }

        internal override void Dispose()
        {             
            _playingInstances.Clear();
            Clip.UnloadAudioData();										            
        }

        internal void AddInstance(SoundClipInstance instance)
        {
            _playingInstances.Add(instance);
            EmitterManager.AddSoundInstance(instance);
        }

        internal void RemoveInstance(SoundClipInstance instance)
        {
            _playingInstances.Remove(instance);
            EmitterManager.RemoveSoundInstance(instance);
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

            SoundClipInstance sci;
            var randomContainer = ParentContainer as SoundRandomContainer;
            if (randomContainer && randomContainer.RandomOnLoop)
                sci = soundSource.AddComponent<RandomLoopClipsInstance>();
            else
                sci = soundSource.AddComponent<SoundClipInstance>();
            sci.Init(this, soundSource);            
            sci.Play(fadeInTime, endCallback);
            return Clip.name;
        }

        public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.Stop(fadeOutTime);                
                }                                                       
            }                        
        }

        internal override void StopAll(float fadeOutTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {                                
                sci.Stop(fadeOutTime);
            }
        }

        internal override void Mute(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.Mute(fadeOutTime);                
                }                                                       
            }  
        }

        internal override void UnMute(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.UnMute(fadeInTime);                
                }                                                       
            }  
        }

        internal override void Pause(GameObject soundSource, float fadeOutTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.Pause(fadeOutTime);                
                }                                                       
            }  
        }

        internal override void Resume(GameObject soundSource, float fadeInTime = 0f)
        {
            foreach (var sci in _playingInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.Resume(fadeInTime);                
                }                                                       
            }  
        }
        #endregion
    }

    public class SoundClipInstance : AudioEventInstance
    {
        #region Initialize
        internal SoundClip SoundClip;
        internal string Name => SoundClip ? SoundClip.Name : string.Empty;

        internal virtual void Init(SoundClip clip, GameObject emitter)
        {
            AudioSource = gameObject.AddComponent<AudioSource>();
            SoundClip = clip;
            Emitter = emitter;
            
            SoundClip.AddInstance(this);
            InitAudioSource(AudioSource);
            InitFilters();
            foreach (var mapping in SoundClip.Mappings)
            {
                mapping.Init(this, Emitter);
            }
        }
        
        private void OnDisable()
        {
            OnAudioEndOrStop();
        }

        private void OnDestroy()
        {            
            foreach (var mapping in SoundClip.Mappings)
            {
                mapping.Dispose(Emitter);
            }
            
            OnAudioEnd?.Invoke(Emitter);
            if (SoundClip.EnableVoiceLimit && SoundClip.VoiceLimiter)
                SoundClip.VoiceLimiter.RemoveVoice(AudioVoiceInstance);
            
            if (AudioSource)
                Destroy(AudioSource);
            SoundClip.RemoveInstance(this);
        }

        protected void InitAudioSource(AudioSource source)
        {
            source.clip = SoundClip.Clip;
            source.loop = SoundClip.Loop;
            source.panStereo = SoundClip.Pan;
            source.volume = SoundClip.RandomizeVolume ? GetRandomValue(SoundClip.Volume, SoundClip.VolumeRandomRange) : SoundClip.Volume;
            source.pitch = SoundClip.RandomizePitch ? GetRandomValue(SoundClip.Pitch, SoundClip.PitchRandomRange) : SoundClip.Pitch;
            source.outputAudioMixerGroup = AudioManager.GetAudioMixer("SFX", SoundClip.AudioMixer);
            if (SoundClip.Is3D)
                ApplySpatialSettings(source);
        }

        protected void InitFilters()
        {
            if (SoundClip.LowPassFilter)
            {
                LowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                LowPassFilter.cutoffFrequency = SoundClip.LowPassCutoff;
                LowPassFilter.lowpassResonanceQ = SoundClip.LowPassResonance;
            }
            if (SoundClip.HighPassFilter)
            {
                HighPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
                HighPassFilter.cutoffFrequency = SoundClip.HighPassCutoff;
                HighPassFilter.highpassResonanceQ = SoundClip.HighPassResonance;
            }
        }

        private void ApplySpatialSettings(AudioSource source)
        {			
            source.rolloffMode = SoundClip.RollOffMode;				
            source.minDistance = SoundClip.MinDistance;
            source.maxDistance = SoundClip.MaxDistance;
            source.spatialBlend = SoundClip.SpatialBlend;
            source.spread = SoundClip.SpreadWidth;
            if (SoundClip.RollOffMode == AudioRolloffMode.Custom)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, SoundClip.AttenuationCurve);
        }
        
        private static float GetRandomValue(float baseValue, float randomRange)
        {
            return baseValue + Random.Range(-randomRange, randomRange);
        }
        #endregion
        
        #region Editor
        private void OnDrawGizmosSelected()
        {
            if (!SoundClip.Is3D) return;
            
            switch (AudioPathSettings.Instance.GizmosSphereColor)
            {
                case GizmosColor.Disabled:
                    return;
                case GizmosColor.Red:
                    Gizmos.color = new Color(1, 0, 0, 0.2f);
                    break;
                case GizmosColor.Yellow:
                    Gizmos.color = new Color(1, 1, 0, 0.2f);
                    break;
                case GizmosColor.Green:
                    Gizmos.color = new Color(0, 1, 0, 0.2f);
                    break;
                case GizmosColor.Blue:
                    Gizmos.color = new Color(0, 0, 1, 0.2f);
                    break;
            }
            Gizmos.DrawSphere(transform.position, SoundClip.MaxDistance);
        }
        #endregion

        #region Playback        
        protected AudioVoiceInstance AudioVoiceInstance;

        internal void Play(float fadeInTime, Action<GameObject> endCallback = null)
        {
            if (!CheckVoiceLimiter())
                return;

            OnAudioEnd = endCallback;
            SeekPositionAndPlay(fadeInTime);
        }

        private bool CheckVoiceLimiter()
        {
            if (SoundClip.EnableVoiceLimit && SoundClip.VoiceLimiter)
            {
                AudioVoiceInstance = new AudioVoiceInstance {SoundClipInstance = this, Priority = SoundClip.Priority, PlayTime = Time.time};
                return SoundClip.VoiceLimiter.AddVoice(AudioVoiceInstance);                              
            }
            return true;
        }

        protected void SeekPositionAndPlay(float fadeInTime)
        {
            if (SoundClip.SeekRandomPosition)
            {
                var startSample = Random.Range(0, SoundClip.Clip.samples);
                AudioSource.timeSamples = startSample;
            }
            PlayingStatus = PlayingStatus.Playing;
            if (isActiveAndEnabled && fadeInTime > 0f)
                StartCoroutine(AudioSource.Play(fadeInTime));
            else
                AudioSource.Play();
        }

        internal override void UpdatePlayingStatus()
        {
            if (PlayingStatus != PlayingStatus.Playing) return;                
            
            if (AudioSource.timeSamples < TimeSamples)
            {                                        
                if (AudioSource.isPlaying) //loop back
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Loop, AudioTriggerSource.Code, SoundClip.name, gameObject);
                else //finish playing
                {
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.End, AudioTriggerSource.Code, SoundClip.name, gameObject);
                    OnAudioEndOrStop();
                }
            }
                        
            //update play head sample
            TimeSamples = AudioSource.timeSamples;
        }
        #endregion

        #region Controls
        
        #endregion
    }

    public class RandomLoopClipsInstance : SoundClipInstance
    {
        private SoundRandomContainer _clipPool;
        private SoundClip _originalClip;
        private AudioSource _source1;
        private AudioSource _source2;
        private float _volume;
        private int CrossFadeStartSample => Mathf.CeilToInt(SoundClip.Clip.samples - _clipPool.CrossFadeTime * SoundClip.Clip.frequency * Mathf.Abs(SoundClip.Pitch));
        
        private void OnDestroy()
        {            
            foreach (var mapping in _clipPool.Mappings)
            {
                mapping.Dispose(Emitter);
            }
            
            OnAudioEnd?.Invoke(Emitter);
            if (SoundClip.EnableVoiceLimit && SoundClip.VoiceLimiter)
                SoundClip.VoiceLimiter.RemoveVoice(AudioVoiceInstance);
            
            Destroy(_source1);
            Destroy(_source2);
            _originalClip.RemoveInstance(this);
        }

        internal override void Init(SoundClip clip, GameObject emitter)
        {
            AudioSource = _source1 = gameObject.AddComponent<AudioSource>();
            _source2 = gameObject.AddComponent<AudioSource>();
            Emitter = emitter;
            SoundClip = _originalClip = clip;
            
            _originalClip.AddInstance(this);
            _clipPool = clip.ParentContainer as SoundRandomContainer;
            _volume = clip.Volume;
            InitAudioSource(_source1);
            InitAudioSource(_source2);
            InitFilters();
        }

        internal override void UpdatePlayingStatus()
        {
            if (PlayingStatus != PlayingStatus.Playing) return;    
            
            //random to next loop
            if (TimeSamples >= CrossFadeStartSample)
            {                
                SoundClip = _clipPool.GetChildByPlayLogic(gameObject) as SoundClip;
                PlayAgain();
            }
            TimeSamples = AudioSource.timeSamples;
        }

        private void PlayAgain()
        {
            if (isActiveAndEnabled && _clipPool.CrossFadeTime > 0f)
                StartCoroutine(AudioSource.Stop(_clipPool.CrossFadeTime));
            else
                AudioSource.Stop();
            AudioSource = AudioSource == _source1 ? _source2 : _source1;
            AudioSource.volume = _volume;
            AudioSource.clip = SoundClip.Clip;
            AudioSource.panStereo = SoundClip.Pan;
            AudioSource.pitch = SoundClip.Pitch;
            SeekPositionAndPlay(_clipPool.CrossFadeTime);		
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Loop, AudioTriggerSource.Code, SoundClip.name, Emitter);
        }

        #region Controls

        internal override void SetOutputBus(AudioMixerGroup amg)
        {
            _source1.outputAudioMixerGroup = amg;
            if (_source2)
                _source2.outputAudioMixerGroup = amg;
        }

        internal override void SetVolume(float volume)
        {
            AudioSource.volume = _volume = volume;				
        }

        internal override void SetPan(float pan)
        {
            _source1.panStereo = pan;
            if (_source2)
                _source2.panStereo = pan;	
        }

        internal override void SetPitch(float pitch)
        {
            _source1.pitch = pitch;
            if (_source2)
                _source2.pitch = pitch;	
        }
        #endregion
    }
}

