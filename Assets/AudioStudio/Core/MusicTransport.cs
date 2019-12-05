using System;
using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio
{
    #region Enums
    public enum PlayingStatus
    {
        PreEntry,
        Playing,
        PreEntryAgain,
        PendingPostExit,
        PostExit,
        Stopping,
        Idle
    }

    public enum TransitioningStatus
    {
        None,
        PendingTransition,
        PendingSwitch,
        PendingSequence,
        Transitioning           
    }
    
    public enum BeatDuration
    {
        _4,
        _8,
        _16
    }
        
    public enum TransitionInterval
    {
        Immediate,
        NextBeat,
        NextBar,
        NextGrid,
        ExitCue,            
    }
    #endregion
    
    [Serializable]
    public class TransportData //this class stores the current playing music rhythm information
    {
        public TransportData(MusicTrack music)
        {
            MusicTrack = music;
            RemainingLoops = music.LoopCount;
        }
        
        public MusicTrack MusicTrack;
        private MusicMarker[] Markers => MusicTrack.Markers;
        private int _currentMarkerIndex;
        public MusicMarker CurrentMarker => Markers[_currentMarkerIndex];
        public int NextMarkerPosition => _currentMarkerIndex + 1 >= Markers.Length ? MusicTrack.ExitPosition.Bar : Markers[_currentMarkerIndex + 1].BarNumber;
        public int PickUpLengthSamples => MusicTrack.UseDefaultLoopStyle ? 0 : Mathf.FloorToInt(MusicTrack.PickupBeats * Markers[0].BeatDurationRealtime() * SampleRate);
        public int SampleRate => MusicTrack.Clip.frequency;
        public byte RemainingLoops;

        public void GoToNextMarker()
        {
            if (_currentMarkerIndex + 1 < Markers.Length)
                _currentMarkerIndex++;
        }
        
        public void ResetMarker()
        {
            _currentMarkerIndex = 0;
        }
    }        
    
    public class MusicTransport : MonoBehaviour
    {
        #region Instance
        private static MusicTransport _instance;
        public static MusicTransport Instance
        {
            get
            {
                if (_instance) return _instance;
                var go = new GameObject("Music Transport");
                _instance = go.AddComponent<MusicTransport>();
                DontDestroyOnLoad(_instance);
                return _instance;
            }
        }
        public static GameObject GameObject => Instance.gameObject;
        #endregion

        #region Properties
        private bool UseDefaultLoopStyle => ActiveMusicData.MusicTrack.UseDefaultLoopStyle;
        private byte LoopCount => ActiveMusicData.MusicTrack.LoopCount;
        private byte RemainingLoops
        {
            get => ActiveMusicData.RemainingLoops;
            set => ActiveMusicData.RemainingLoops = value;
        }
        private int SampleRate => ActiveMusicData.SampleRate;
        public MusicMarker CurrentMarker => ActiveMusicData.CurrentMarker;
        public BarAndBeat ExitPosition => ActiveMusicData.MusicTrack.ExitPosition;
        private int BeatsPerBar => ActiveMusicData.CurrentMarker.BeatsPerBar;
        private int BeatDurationSamples => Mathf.FloorToInt(CurrentMarker.BeatDurationRealtime() * SampleRate);
        private int PickUpLengthSamples => ActiveMusicData.PickUpLengthSamples;
        private int ExitPositionSamples => Mathf.FloorToInt(ActiveMusicData.MusicTrack.LoopDurationRealTime() * SampleRate) + PickUpLengthSamples;
        private int TrackLengthSamples => ActiveMusicData.MusicTrack.Clip.samples;
        public MusicKey CurrentKey => CurrentMarker.KeyCenter;
        private TransitionInterval TransitionInterval => ActiveMusicData.MusicTrack.TransitionInterval;
        private BarAndBeat GridLength => ActiveMusicData.MusicTrack.GridLength;
        private int GridLengthSamples
        {
            get
            {
                switch (ActiveMusicData.MusicTrack.TransitionInterval)
                {
                    case TransitionInterval.NextBeat:                    
                        return BeatDurationSamples;
                    case TransitionInterval.NextBar:                    
                        return BeatDurationSamples * CurrentMarker.BeatsPerBar;
                    case TransitionInterval.NextGrid:
                        return GridLength.ToBeats(CurrentMarker.BeatsPerBar) * BeatDurationSamples;
                    case TransitionInterval.ExitCue:
                        return TrackLengthSamples;
                    default:
                        return 0;
                }
            }
        }
        private int CurrentSample => PlayHeadAudioSource.timeSamples;
        #endregion
        
        #region PlayEvent
        public TransportData ActiveMusicData;
        public TransportData QueuedMusicData;
        public MusicContainer CurrentPlayingEvent;    
        public MusicContainer NextPlayingEvent;
        public List<MusicTrack> ActiveTracks = new List<MusicTrack>();
        public List<MusicTrack> QueuedTracks = new List<MusicTrack>();        
        public List<MusicTrackInstance> PlayingTrackInstances = new List<MusicTrackInstance>();
        private List<MusicTrackInstance> _exitingTrackInstances = new List<MusicTrackInstance>();
        
        private float _fadeInTime;
        private float _fadeOutTime;
        
        public void SetMusicQueue(MusicContainer evt, float fadeInTime, float fadeOutTime, float exitOffset = -1f, float entryOffset = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {            
            if (fadeInTime < 0f) fadeInTime = evt.DefaultFadeInTime;              
            if (entryOffset < 0f) entryOffset = evt.DefaultEntryOffset;             
            
            if (!CurrentPlayingEvent) //no music is currently playing
            {
                CurrentPlayingEvent = evt;
                ActiveTracks.Clear();
                GetTracks(evt, ActiveTracks);
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Play, trigger, evt.name);
                if (ActiveTracks.Count == 0) return;
                ActiveMusicData = new TransportData(ActiveTracks[0]);
                Play(fadeInTime);                                
            }
            else if (CurrentPlayingEvent != evt)
            {
                if (fadeOutTime < 0f) fadeOutTime = CurrentPlayingEvent.DefaultFadeOutTime;
                if (exitOffset < 0f) exitOffset = CurrentPlayingEvent.DefaultExitOffset;
                if (TransitioningStatus != TransitioningStatus.None) CancelTransition();                
                NextPlayingEvent = evt;
                QueuedTracks.Clear();
                GetTracks(evt, QueuedTracks);
                
                QueuedMusicData = new TransportData(QueuedTracks[0]);
                PrepareTransition(fadeInTime, fadeOutTime, exitOffset, entryOffset);
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SetQueue, trigger, evt.name, gameObject, TransitionInterval != TransitionInterval.Immediate? "MusicEvent will play on " + TransitionInterval : "");
            }
        }

        private static void GetTracks(MusicContainer evt, List<MusicTrack> trackList)
        {
            var track = evt as MusicTrack; 
            if (track) //if this is single track just get it
            {
                trackList.Add(track);
                return;
            }

            if (evt.PlayLogic == MusicPlayLogic.Layer) //play all tracks together
            {
                foreach (var childEvent in evt.ChildEvents)
                {
                    GetTracks(childEvent, trackList);
                }
            }
            else //choose a track by play logic
            {
                var musicContainer = evt.GetEvent();
                GetTracks(musicContainer, trackList);
            }
        }

        //generate the audio sources
        private void CreateMusicTrackInstances(float fadeInTime, int timeSamples = 0)
        {            
            PlayingTrackInstances = new List<MusicTrackInstance>();
            foreach (var track in ActiveTracks)
            {
                var mti = gameObject.AddComponent<MusicTrackInstance>();                     
                mti.Init(track);
                mti.Play(fadeInTime, timeSamples);                    
            }
            PlayHeadAudioSource = PlayingTrackInstances[0].AudioSource;
        }
        #endregion
        
        #region Stinger
        private MusicStinger _queuedStinger;
        private int _triggerStingerSampleStamp;
        
        //play a stinger on top of currently playing tracks
        public void QueueStinger(MusicStinger stinger)
        {            
            _queuedStinger = stinger;
            switch (stinger.TransitionInterval)
            {
                case TransitionInterval.Immediate:
                    PlayStinger();
                    break;
                case TransitionInterval.NextBar:
                    _triggerStingerSampleStamp = Mathf.FloorToInt(_beatSampleStamp + BeatsPerBar * BeatDurationSamples - stinger.PickUpLength * SampleRate);                                         
                    while (CurrentSample >= _triggerStingerSampleStamp)
                    {
                        _triggerStingerSampleStamp += BeatsPerBar * BeatDurationSamples;
                    }
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SetQueue, AudioTriggerSource.Code, stinger.name, gameObject, "MusicStinger queued on NextBar");
                    break;
                case TransitionInterval.NextBeat:
                    _triggerStingerSampleStamp = Mathf.FloorToInt(_beatSampleStamp + BeatDurationSamples - stinger.PickUpLength * SampleRate);                                            
                    while (CurrentSample >= _triggerStingerSampleStamp)
                    {
                        _triggerStingerSampleStamp += BeatDurationSamples;
                    }
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SetQueue, AudioTriggerSource.Code, stinger.name, gameObject, "MusicStinger queued on NextBeat");
                    break;
            }            
        }
        
        private void PlayStinger()
        {
            _triggerStingerSampleStamp = 0;
            if (PlayingTrackInstances.Count == 0)
            {
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Music, AudioAction.Stinger, AudioTriggerSource.Code, _queuedStinger.name, gameObject, "No music is playing, MusicStinger won't play");
                return;
            }

            foreach (var keyAssignment in _queuedStinger.KeyAssignments)
            {
                if ((keyAssignment.Keys & CurrentKey) != MusicKey.None)
                    PlayHeadAudioSource.PlayOneShot(keyAssignment.Clip, _queuedStinger.Volume);       
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Stinger, AudioTriggerSource.Code, _queuedStinger.name, gameObject, CurrentKey.ToString());
        }
        #endregion

        #region PlayingStatus
        public PlayingStatus PlayingStatus = PlayingStatus.Idle;
        public Action PlayCallback;
        public Action LoopCallback;
        public Action ExitCallback;
        public Action StopCallback;
        public Action BeatCallback;
        public Action BarCallback;
        
        public void Play(float fadeInTime)
        {                      
            CreateMusicTrackInstances(fadeInTime);              
            PreEntry();        
            PlayCallback?.Invoke();            
        }

        private void PreEntry()
        {
            _beatSampleStamp = 0;
            if (UseDefaultLoopStyle || PickUpLengthSamples == 0) //no pre-entry needed
            {
                OnLoopStartPosition();
                return;
            }

            PlayingStatus = PlayingStatus.PreEntry;
            var pickupBeats = Mathf.CeilToInt(ActiveMusicData.MusicTrack.PickupBeats);
            PlayHeadPosition = BarAndBeat.ToBarAndBeat(pickupBeats, BeatsPerBar).Negative(BeatsPerBar);
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.PreEntry, AudioTriggerSource.Code, ActiveTracks[0].name, gameObject);
        }
        
        private void OnLoopStartPosition()
        {            
            PlayingStatus = PlayingStatus.Playing;            
            PlayHeadPosition = BarAndBeat.Zero;            
            _beatSampleStamp = _transitionGridSampleStamp = PickUpLengthSamples;
            foreach (var track in ActiveTracks)
            {
                track.OnPlay();
            }
            LoopCallback?.Invoke();
            
            if (RemainingLoops != 1) //more loops to go
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Loop, AudioTriggerSource.Code, ActiveTracks[0].name, gameObject);
            else if (CurrentPlayingEvent.PlayLogic == MusicPlayLogic.SequenceContinuous)            
                SetSequence(CurrentPlayingEvent.GetNextEvent());            
        }

        private void OnPreEntryAgainPosition()
        {
            if (LoopCount != 0) //definite number of loops
            {
                RemainingLoops--;
                if (RemainingLoops == 0) //all loops are finished
                {
                    PlayingStatus = PlayingStatus.PendingPostExit;  
                    return;
                }                                                        
            }                       
            PlayingStatus = PlayingStatus.PreEntryAgain;     
            ActiveMusicData.ResetMarker();
            _beatSampleStamp = 0;
            foreach (var msi in PlayingTrackInstances)
            {
                msi.Play(0);
            }
            PlayHeadAudioSource = PlayingTrackInstances[0].AudioSource;                                                             
        }

        private void OnDefaultLoopStyleEndPosition()
        {
            if (TransitioningStatus == TransitioningStatus.PendingTransition) //if we need to play the next music
            {
                if (_exitFirst) 
                    TransitionExit();
                else
                    TransitionEnter();
                return;
            }
            
            if (LoopCount != 0) //if definite number of loops
            {
                RemainingLoops--;
                if (RemainingLoops == 0) //all loops are finished
                {
                    Stop();
                    return;
                }                                                         
            }  
            OnLoopStartPosition(); //play from top again                          
        }
        
        private void OnPostExitPosition() //finish the last loop
        {            
            PlayingStatus = PlayingStatus.PostExit;
            Invoke(nameof(Stop), SamplesToTime(TrackLengthSamples - ExitPositionSamples));
            ExitCallback?.Invoke();
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.PostExit, AudioTriggerSource.Code, ActiveTracks[0].name, gameObject);
        }

        public void Stop(float fadeOutTime = 0f)
        {
            if (PlayingStatus == PlayingStatus.Idle)
                return;
            PlayingStatus = PlayingStatus.Stopping;
            foreach (var instance in PlayingTrackInstances)
            {
                if (instance) instance.Stop(fadeOutTime);                                
            }
            foreach (var instance in _exitingTrackInstances)
            {
                if (instance) instance.Stop(fadeOutTime);                
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.StopEvent, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject, _fadeOutTime + "s fade out");
            Invoke(nameof(ResetAll), fadeOutTime);  
        }

        private void ResetAll() //reset music transport to idle
        {            
            PlayingStatus = PlayingStatus.Idle;
            CancelTransition();
            PlayHeadPosition = BarAndBeat.Zero;            
            CurrentPlayingEvent = null;
            NextPlayingEvent = null;
            PlayingTrackInstances.Clear();  
            _exitingTrackInstances.Clear();
            ActiveMusicData = null;
            QueuedMusicData = null;
            StopCallback?.Invoke();
        }
        #endregion

        #region Transition       
        public TransitioningStatus TransitioningStatus = TransitioningStatus.None;
        public int TransitionExitSampleStamp;
        public int TransitionEnterSampleStamp;
        private int _transitionGridSampleStamp;
        private bool _exitFirst = true;
        
        private void PrepareTransition(float fadeInTime, float fadeOutTime, float exitOffset, float entryOffset)
        {
            _fadeInTime = fadeInTime;
            _fadeOutTime = fadeOutTime;         
            
            //See if there are any segments connecting
            var segment = AsAssetLoader.GetTransitionSegment(CurrentPlayingEvent, NextPlayingEvent);
            if (segment)
            {
                SetMusicQueue(segment, segment.OriginFadeOutTime, segment.SegmentFadeInTime);                    
                return;                
            }

            //Calculate the sample to exit and enter
            switch (TransitionInterval)
            {
                case TransitionInterval.Immediate:
                    TransitionEnterSampleStamp = PlayHeadAudioSource.timeSamples + Mathf.Max(0, Mathf.FloorToInt(entryOffset * SampleRate));
                    TransitionExitSampleStamp = PlayHeadAudioSource.timeSamples + Mathf.Max(0, Mathf.FloorToInt(exitOffset * SampleRate));
                    break;
                case TransitionInterval.ExitCue:
                    TransitionEnterSampleStamp = ExitPositionSamples - 
                                                  QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(entryOffset * SampleRate);
                    TransitionExitSampleStamp = ExitPositionSamples + Mathf.FloorToInt(exitOffset * SampleRate);
                    break;
                default:
                    TransitionEnterSampleStamp = _transitionGridSampleStamp + GridLengthSamples - 
                                                  QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(entryOffset * SampleRate);
                    TransitionExitSampleStamp = _transitionGridSampleStamp + GridLengthSamples 
                                                                            + Mathf.FloorToInt(exitOffset * SampleRate);                
                    while (TransitionEnterSampleStamp < PlayHeadAudioSource.timeSamples)
                    {
                        TransitionEnterSampleStamp += GridLengthSamples;
                        TransitionExitSampleStamp += GridLengthSamples;
                    }             
                    break;
            }

            _exitingTrackInstances = PlayingTrackInstances;
            _exitFirst = TransitionExitSampleStamp <= TransitionEnterSampleStamp;          
            TransitioningStatus = TransitioningStatus.PendingTransition;
        }

        private void CancelTransition()
        {
            TransitioningStatus = TransitioningStatus.None;
            CancelInvoke(nameof(TransitionEnter));
            CancelInvoke(nameof(TransitionExit));
        }

        private void TransitionEnter() //new music starts playing
        {            
            CurrentPlayingEvent = NextPlayingEvent;
            NextPlayingEvent = null;
            ActiveTracks = QueuedTracks;
            QueuedTracks = new List<MusicTrack>();
            ActiveMusicData = QueuedMusicData;
            QueuedMusicData = null;
            
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionEnter, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject, _fadeInTime + "s fade in");
            CreateMusicTrackInstances(_fadeInTime);            
            PreEntry();

            if (!_exitFirst)
            {
                Invoke(nameof(TransitionExit), SamplesToTime(TransitionExitSampleStamp - TransitionEnterSampleStamp));
                TransitioningStatus = TransitioningStatus.Transitioning;
            }
            else if (TransitioningStatus == TransitioningStatus.Transitioning)           
                TransitioningStatus = TransitioningStatus.None;
        }

        private void TransitionExit() //old music finishes playing
        {                        
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionExit, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject, _fadeOutTime + "s fade out");
            foreach (var mti in _exitingTrackInstances)
            {
                mti.Stop(_fadeOutTime);                
            }
            _exitingTrackInstances.Clear();    
            if (_exitFirst)
            {
                Invoke(nameof(TransitionEnter), SamplesToTime(TransitionEnterSampleStamp - TransitionExitSampleStamp));
                TransitioningStatus = TransitioningStatus.Transitioning;
            }
            else if (TransitioningStatus == TransitioningStatus.Transitioning)
                TransitioningStatus = TransitioningStatus.None;
        }
        #endregion
        
        #region Switch
        private bool _switchToSamePosition;
        private float _crossFadeTime;
        private int _switchSampleStamp;
        
        public void OnSwitchChanged(bool switchImmediately, bool toSamePosition, float crossFadeTime) //audio switch is set to a different value that affects music
        {
            _crossFadeTime = crossFadeTime;
            _switchToSamePosition = toSamePosition;
            if (!switchImmediately)
            {
                switch (TransitionInterval)
                {
                    case TransitionInterval.Immediate:
                        SwitchCrossFade();
                        break;
                    case TransitionInterval.ExitCue:
                        _switchSampleStamp = ExitPositionSamples - PickUpLengthSamples - TimeToSamples(crossFadeTime / 2f);
                        TransitioningStatus = TransitioningStatus.PendingSwitch;
                        break;
                    default:
                        _switchSampleStamp = _transitionGridSampleStamp + GridLengthSamples - TimeToSamples(crossFadeTime / 2f);
                        TransitioningStatus = TransitioningStatus.PendingSwitch;
                        break;
                }                    
            }      
            else
                SwitchCrossFade();
        }
        
        private void SwitchCrossFade() //cross fade the two music
        {
            TransitioningStatus = TransitioningStatus.None;                                    
            foreach (var msi in PlayingTrackInstances)
            {
                msi.Stop(_crossFadeTime);                
            }          
            ActiveTracks.Clear();            
            GetTracks(CurrentPlayingEvent, ActiveTracks);
            ActiveMusicData = new TransportData(ActiveTracks[0]);
            if (_switchToSamePosition)
            {
                CreateMusicTrackInstances(_crossFadeTime, PlayHeadAudioSource.timeSamples);
            }
            else
            {
                CreateMusicTrackInstances(_crossFadeTime);
                PreEntry();
            }       
        }
        #endregion
        
        #region SequenceContinuous

        private void SetSequence(MusicContainer nextMusic) //get the next item of a sequence continuous music container
        {
            if (!nextMusic) return; //If reaches the last item in sequence            
            TransitioningStatus = TransitioningStatus.PendingSequence;                          
            QueuedTracks.Clear();
            GetTracks(nextMusic, QueuedTracks);
            
            QueuedMusicData = new TransportData(QueuedTracks[0]);                    
            TransitionEnterSampleStamp = ExitPositionSamples - QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(nextMusic.DefaultEntryOffset * SampleRate);
            TransitionExitSampleStamp = TrackLengthSamples;
            _exitingTrackInstances = PlayingTrackInstances;
        }
        
        private void SequenceEnter() //next sequence track plays
        {   
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SequenceEnter, AudioTriggerSource.Code, QueuedTracks[0].name, gameObject);
            ActiveTracks = QueuedTracks;
            QueuedTracks = new List<MusicTrack>();
            ActiveMusicData = QueuedMusicData;
            QueuedMusicData = null;
            
            CreateMusicTrackInstances(_fadeInTime);            
            PreEntry();
            Invoke(nameof(SequenceExit), SamplesToTime(TransitionExitSampleStamp - TransitionEnterSampleStamp));
            TransitioningStatus = TransitioningStatus.Transitioning;
        }
        
        private void SequenceExit() //old sequence track finishes
        {
            foreach (var mti in _exitingTrackInstances)
            {
                mti.Stop(_fadeOutTime);                
            }
            _exitingTrackInstances.Clear();  
            if (TransitioningStatus == TransitioningStatus.Transitioning) 
                TransitioningStatus = TransitioningStatus.None;
        }
        #endregion

        #region PlayHead	
        private int _beatSampleStamp;
        public float TimeSamples;
        public BarAndBeat PlayHeadPosition;
        public AudioSource PlayHeadAudioSource;
        
        private void FixedUpdate()
        {
            if (!PlayHeadAudioSource || ActiveMusicData == null) return;
            
            //if beat offset is positive, beat is delayed
            TimeSamples = PlayHeadAudioSource.timeSamples;
            
            //sometimes music is delayed one frame, fix it
            var frameSamples = (int)(Time.fixedDeltaTime * SampleRate);
            
            //stinger queue time stamp is reached
            if (_triggerStingerSampleStamp > 0 && _triggerStingerSampleStamp - TimeSamples < frameSamples) 
                PlayStinger();    
            
            if (_beatSampleStamp + BeatDurationSamples - TimeSamples < frameSamples) 
                OnBeat();
            
            switch (TransitioningStatus)
            {
                case TransitioningStatus.Transitioning:
                    return;
                case TransitioningStatus.PendingSwitch:
                    if (_switchSampleStamp - TimeSamples < frameSamples) 
                        SwitchCrossFade();
                    break;
                case TransitioningStatus.PendingTransition:
                    if (TransitionExitSampleStamp - TimeSamples < frameSamples)
                    {                                     
                        TransitionExit();
                        return;
                    } 
                    if (TransitionEnterSampleStamp - TimeSamples < frameSamples)                
                    {
                        TransitionEnter();
                        return;
                    } 
                    break;
                case TransitioningStatus.PendingSequence:
                    if (TransitionEnterSampleStamp - TimeSamples < frameSamples)                
                    {                                                          
                        SequenceEnter();
                        return;
                    } 
                    break;
            }

            //checking for exit or loop point
            switch (PlayingStatus)
            {
                case PlayingStatus.Playing:
                case PlayingStatus.PreEntryAgain:
                case PlayingStatus.PendingPostExit:
                    if (_transitionGridSampleStamp + GridLengthSamples - TimeSamples < frameSamples)
                        OnTransitionGrid();
                    break;
            }

            if (!UseDefaultLoopStyle)
            {
                switch (PlayingStatus)
                {
                    case PlayingStatus.Playing: //prepare to entry again
                        if (ExitPositionSamples - PickUpLengthSamples - TimeSamples < frameSamples)
                            OnPreEntryAgainPosition();
                        break;
                    case PlayingStatus.PreEntry: //enter loop body
                    case PlayingStatus.PreEntryAgain:
                        if (PickUpLengthSamples - TimeSamples < frameSamples)
                            OnLoopStartPosition();
                        break;
                    case PlayingStatus.PendingPostExit: //finish loop body                        
                        if (ExitPositionSamples - TimeSamples < frameSamples)
                            OnPostExitPosition();
                        break;
                }                                                                                
            }
            else if (PlayingStatus == PlayingStatus.Playing && _beatSampleStamp - TimeSamples > frameSamples)            
                 OnDefaultLoopStyleEndPosition();            
        }
        
        private void OnBeat()
        {
            _beatSampleStamp += BeatDurationSamples;
            PlayHeadPosition.Beat++;                                     
            if (PlayHeadPosition.Beat == BeatsPerBar)
            {
                PlayHeadPosition.Bar++;
                PlayHeadPosition.Beat = 0;
                if (PlayHeadPosition.Bar >= ActiveMusicData.NextMarkerPosition)
                    ActiveMusicData.GoToNextMarker();
                BarCallback?.Invoke();
            }
            BeatCallback?.Invoke();
        }

        private void OnTransitionGrid()
        {                       
            _transitionGridSampleStamp += GridLengthSamples;
        }

        private float SamplesToTime(int samples)
        {
            return samples / Mathf.Abs(CurrentPlayingEvent.Pitch) / SampleRate;
        }
        
        private int TimeToSamples(float time)
        {
            return Mathf.FloorToInt(time * Mathf.Abs(CurrentPlayingEvent.Pitch) * SampleRate);
        }
        #endregion

        #region Controls
        public void Pause(float fadeOutTime)
        {
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.Pause(fadeOutTime);
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Pause, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject);
        }
        
        public void Resume(float fadeInTime)
        {
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.Resume(fadeInTime);
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Resume, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject);
        }
        
        public void Mute(float fadeOutTime)
        {            
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.Mute(fadeOutTime);
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Mute, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject);
        }
        
        public void UnMute(float fadeInTime)
        {            
            foreach (var mti in PlayingTrackInstances)
            {			                
                mti.UnMute(fadeInTime);                
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Unmute, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject);
        }
        
        public void SetOutputBus(AudioMixerGroup bus)
        {
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.SetOutputBus(bus);
            }
        }
                
        public void SetVolume(float volume)
        {            
            volume = Mathf.Clamp01(volume);
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.SetVolume(volume);
            }
        }
        
        public void SetPan(float pan)
        {            
            pan = Mathf.Clamp(pan, -1f, 1f);
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.SetPan(pan);
            }
        }
                
        public void SetLowPassCutoff(float cutoff)
        {            
            cutoff = Mathf.Clamp(cutoff, 10f, 22000f);
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.SetLowPassCutoff(cutoff);
            }
        }
                
        public void SetHighPassCutoff(float cutoff)
        {            
            cutoff = Mathf.Clamp(cutoff, 10f, 22000f);
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.SetHighPassCutoff(cutoff);
            }
        }
        #endregion
    }   
}