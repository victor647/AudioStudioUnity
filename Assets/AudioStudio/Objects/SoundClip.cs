using System;
using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Clip", menuName = "Audio/Sound/Clip")]
    public class SoundClip : SoundContainer
    {      
        #region Fields
        public AudioClip Clip;        
        public bool Loop;
        public bool SeekRandomPosition;
        [NonSerialized]
        public List<SoundClipInstance> SoundClipInstances;        
        
        #endregion
        
        #region Editor
        public override void CleanUp()
        {            
            ChildEvents = null;            
            SwitchEventMappings = null;
            if (!Clip)
                Debug.LogError("AudioClip of SoundClip " + name + " is missing!");
        }
        
        public override bool IsValid()
        {
            return Clip != null;
        }
        #endregion        
        
        #region Initialize
        public override void Init()
        {            
            SoundClipInstances = new List<SoundClipInstance>();
            Clip.LoadAudioData();									                   
        }
        
        public override void Dispose()
        {             
            SoundClipInstances.Clear();
            //Clip.UnloadAudioData();										            
        }
        #endregion

        #region Playback                        
        public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AsUnityHelper.PlayAudioClipOffline(Clip);
                return;
            }
#endif
            
            if (fadeInTime < 0) fadeInTime = DefaultFadeInTime;
            
            if (EnableVoiceLimit)
            {
                AddVoicing(soundSource);
                endCallback += RemoveVoicing;
            }  
                                                
            SoundClipInstance sci;
            if (ParentContainer && ParentContainer.RandomOnLoop)
                sci = soundSource.AddComponent<RandomLoopClipsInstance>();
            else
                sci = soundSource.AddComponent<SoundClipInstance>();
            sci.Init(this, soundSource);            
            sci.Play(fadeInTime, endCallback);                        
        }
        


        public override void Stop(GameObject soundSource, float fadeOutTime)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                AsUnityHelper.StopAudioClipOffline(Clip);
                return;
            }
#endif   
            
            if (fadeOutTime < 0) fadeOutTime = DefaultFadeOutTime;            
                        
            foreach (var sci in SoundClipInstances)
            {
                if (sci.Emitter == soundSource)
                {                    
                    sci.Stop(fadeOutTime);                
                }                                                       
            }                        
        }

        public override void StopAll(float fadeOutTime)
        {            
            if (fadeOutTime < 0) fadeOutTime = DefaultFadeOutTime; 
            
            foreach (var sci in SoundClipInstances)
            {                                
                sci.Stop(fadeOutTime);
            }            
        }
        #endregion
    }

    public class SoundClipInstance : AudioEventInstance
    {
        public static int GlobalSoundCount;
        
        #region Initialize
        public string Name => SoundClip ? SoundClip.Name : string.Empty;

        private void Awake()
        {
            AudioSource = gameObject.AddComponent<AudioSource>();
            GlobalSoundCount++;            
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
            
            SoundClip.SoundClipInstances.Remove(this);
            GlobalSoundCount--;
        }

        public virtual void Init(SoundClip clip, GameObject emitter)
        {
            SoundClip = clip;
            Emitter = emitter;
            clip.SoundClipInstances.Add(this);
            
            InitAudioSource(AudioSource);
            InitFilters();
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

            foreach (var mapping in SoundClip.Mappings)
            {
                mapping.Init(this, Emitter);
            }
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
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, SoundClip.AttenuationCurve);
        }
        
        private static float GetRandomValue(float baseValue, float randomRange)
        {
            return baseValue + Random.Range(-randomRange, randomRange);
        }
        #endregion

        #region Playback        
        protected AudioVoiceInstance AudioVoiceInstance;
        protected SoundClip SoundClip;        
        
        public void Play(float fadeInTime, Action<GameObject> endCallback = null)
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
                AudioVoiceInstance = new AudioVoiceInstance {SoundClipInstance = this, Priority = SoundClip.Priority, PlayTime = Time.time, Distance = GetDistance()};
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
                var message = "Loop starts at position " + startSample * 100 / SoundClip.Clip.samples + "%";
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, AudioTriggerSource.Code, SoundClip.name, gameObject, message);
            } 
            else
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Play, AudioTriggerSource.Code, SoundClip.name, gameObject, SoundClip.Loop ? "Loop" : "");
            PlayingStatus = PlayingStatus.Playing;  
            StartCoroutine(AudioSource.Play(fadeInTime));
        }

        private float GetDistance()
        {
            return SoundClip.SpatialBlend == 0f ? 0f : ListenerManager.GetListenerDistance(gameObject);
        }

        private void FixedUpdate()
        {
            if (PlayingStatus != PlayingStatus.Playing) return;                
            
            if (AudioSource.timeSamples < TimeSamples)
            {                                        
                if (AudioSource.isPlaying) //loop back
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Loop, AudioTriggerSource.Code, SoundClip.name, gameObject);
                else //finish playing
                {
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.End, AudioTriggerSource.Code, SoundClip.name, gameObject);
                    AudioEnd();
                }
            }
                        
            //update play head sample
            TimeSamples = AudioSource.timeSamples;
        }
        
        protected override void AudioEnd()
        {
            PlayingStatus = PlayingStatus.Idle;
            if (gameObject.name.EndsWith("(AudioSource)"))
                Destroy(gameObject);
            else
            {
                Destroy(AudioSource);
                Destroy(this);
            }
        }
        #endregion

        #region Controls
        
        #endregion
    }

    public class RandomLoopClipsInstance : SoundClipInstance
    {
        private SoundContainer _clipPool;
        private SoundClip _originalClip;
        private AudioSource _source1;
        private AudioSource _source2;
        private float _volume;
        private int CrossFadeStartSample => Mathf.CeilToInt(SoundClip.Clip.samples - SoundClip.CrossFadeTime * SoundClip.Clip.frequency * Mathf.Abs(SoundClip.Pitch));
        
        private void Awake()
        {
            _source1 = gameObject.AddComponent<AudioSource>();
            _source2 = gameObject.AddComponent<AudioSource>();
            AudioSource = _source1;
            GlobalSoundCount++;            
        }

        private void OnDestroy()
        {            
            foreach (var mapping in _clipPool.Mappings)
            {
                mapping.Dispose(Emitter);
            }
            
            OnAudioEnd?.Invoke(Emitter);
            if (SoundClip.EnableVoiceLimit && SoundClip.VoiceLimiter)
                SoundClip.VoiceLimiter.RemoveVoice(AudioVoiceInstance);
            
            _originalClip.SoundClipInstances.Remove(this);
            GlobalSoundCount--;
        }
        
        public override void Init(SoundClip clip, GameObject emitter)
        {
            Emitter = emitter;
            SoundClip = _originalClip = clip;
            _originalClip.SoundClipInstances.Add(this);
            _clipPool = clip.ParentContainer;
            _volume = clip.Volume;
            InitAudioSource(_source1);
            InitAudioSource(_source2);
            InitFilters();
        }

        private void FixedUpdate()
        {
            if (PlayingStatus != PlayingStatus.Playing) return;    
            
            //random to next loop
            if (TimeSamples >= CrossFadeStartSample)
            {                
                SoundClip = _clipPool.GetRandomContainer() as SoundClip;
                PlayAgain();
            }
            TimeSamples = AudioSource.timeSamples;
        }

        private void PlayAgain()
        {
            StartCoroutine(AudioSource.Stop(SoundClip.CrossFadeTime));							
            
            AudioSource = AudioSource == _source1 ? _source2 : _source1;
            AudioSource.volume = _volume;
            AudioSource.clip = SoundClip.Clip;
            AudioSource.panStereo = SoundClip.Pan;
            AudioSource.pitch = SoundClip.Pitch;
            SeekPositionAndPlay(SoundClip.CrossFadeTime);		
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.Loop, AudioTriggerSource.Code, SoundClip.name, Emitter);
        }
        
        protected override void AudioEnd()
        {
            PlayingStatus = PlayingStatus.Idle;
            if (gameObject.name.EndsWith("(AudioSource)"))
                Destroy(gameObject);
            else
            {
                Destroy(_source1);
                Destroy(_source2);
                Destroy(this);
            }
        }
        
        #region Controls
        public override void SetOutputBus(AudioMixerGroup amg)
        {
            _source1.outputAudioMixerGroup = amg;
            if (_source2)
                _source2.outputAudioMixerGroup = amg;
        }
		
        public override void SetVolume(float volume)
        {
            AudioSource.volume = _volume = volume;				
        }
		
        public override void SetPan(float pan)
        {
            _source1.panStereo = pan;
            if (_source2)
                _source2.panStereo = pan;	
        }
		
        public override void SetPitch(float pitch)
        {
            _source1.pitch = pitch;
            if (_source2)
                _source2.pitch = pitch;	
        }
        #endregion
    }
}

