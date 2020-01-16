using System;
using System.Collections.Generic;
using System.Linq;
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
        private float FadeInTime => _currentTransitionEntryData.FadeInTime;
        private float EntryOffset => _currentTransitionEntryData.EntryOffset;
        private float FadeOutTime => _currentTransitionExitData.FadeOutTime;
        private float ExitOffset => _currentTransitionExitData.ExitOffset;
        private TransitionInterval TransitionInterval => _currentTransitionExitData.Interval;
        private BarAndBeat GridLength => _currentTransitionExitData.GridLength;
        private int GridLengthSamples
        {
            get
            {
                switch (_currentTransitionExitData.Interval)
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

        public int CurrentSample => PlayHeadAudioSource.timeSamples;
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
        
        private TransitionEntryData _currentTransitionEntryData;
        private TransitionExitData _currentTransitionExitData;
        
        public void SetMusicQueue(MusicContainer newMusic, float fadeInTime = 0f)
        {
            _currentTransitionExitData = GetTransitionExitCondition(newMusic);
            _currentTransitionEntryData = GetTransitionEntryCondition(newMusic);

            if (!CurrentPlayingEvent) //no music is currently playing
            {
                CurrentPlayingEvent = newMusic;
                ActiveTracks.Clear();
                GetTracks(newMusic, ActiveTracks);
                if (ActiveTracks.Count == 0) return;
                ActiveMusicData = new TransportData(ActiveTracks[0]);
                Play(fadeInTime);                                
            }
            else if (CurrentPlayingEvent != newMusic)
            {
                if (TransitioningStatus != TransitioningStatus.None) 
                    CancelTransition();                
                NextPlayingEvent = newMusic;
                QueuedTracks.Clear();
                GetTracks(newMusic, QueuedTracks);
                if (QueuedTracks.Count == 0) return;
                QueuedMusicData = new TransportData(QueuedTracks[0]);
                PrepareTransition();
            }
            _exitingTrackInstances = PlayingTrackInstances;
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
        private void CreateMusicTrackInstances(float fadeInTime = 0f, int timeSamples = 0)
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
            switch (stinger.TriggerSync)
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
                if (RemainingLoops == 0) //all loops are finished, wait for post-exit
                {
                    PlayingStatus = PlayingStatus.PendingPostExit;  
                    return;
                }                                                        
            }                       
            PlayingStatus = PlayingStatus.PreEntryAgain;     
            ActiveMusicData.ResetMarker(); //go back to the first tempo marker
            _beatSampleStamp = 0;
            foreach (var msi in PlayingTrackInstances)
            {
                msi.Play(0); //play from the top again
            }
            PlayHeadAudioSource = PlayingTrackInstances[0].AudioSource; //switching audio source                          
        }

        private void OnDefaultLoopStyleEndPosition()
        {
            if (TransitioningStatus == TransitioningStatus.PendingTransition) //if we need to play the next music
            {
                if (_exitFirst) 
                    TransitionExit();
                else
                    TransitionEnter();
            }
            else
            {
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
        
        private TransitionExitData GetTransitionExitCondition(MusicContainer newMusic)
        {
            if (!CurrentPlayingEvent)
                return new TransitionExitData();
            return CurrentPlayingEvent.TransitionExitConditions.FirstOrDefault(c => c.Target.Name == newMusic.name) ?? 
                   CurrentPlayingEvent.TransitionExitConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Target.Name)) ??
                   new TransitionExitData();
        }
        
        private TransitionEntryData GetTransitionEntryCondition(MusicContainer newMusic)
        {
            if (!CurrentPlayingEvent)
                return newMusic.TransitionEntryConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Source.Name)) ?? new TransitionEntryData();
            return newMusic.TransitionEntryConditions.FirstOrDefault(c => c.Source.Name == CurrentPlayingEvent.name) ?? 
                   newMusic.TransitionEntryConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Source.Name)) ??
                   new TransitionEntryData();
        }
        
        private void PrepareTransition()
        {
            //See if there are any segments connecting
            if (!string.IsNullOrEmpty(_currentTransitionEntryData.TransitionSegment.Name))
            {
                var nextEvent = NextPlayingEvent;
                var segmentFound = false;
                AsAssetLoader.LoadMusic(_currentTransitionEntryData.TransitionSegment.Name, segment =>
                {
                    if (!segment) return;
                    SetMusicQueue(segment);
                    SetSequence(nextEvent);
                    segmentFound = true;
                });
                if (segmentFound) return;
            }

            //Calculate the sample to exit and enter
            switch (TransitionInterval)
            {
                case TransitionInterval.Immediate:
                    TransitionEnterSampleStamp = PlayHeadAudioSource.timeSamples + Mathf.Max(0, Mathf.FloorToInt(EntryOffset * SampleRate));
                    TransitionExitSampleStamp = PlayHeadAudioSource.timeSamples + Mathf.Max(0, Mathf.FloorToInt(ExitOffset * SampleRate));
                    break;
                case TransitionInterval.ExitCue:
                    TransitionEnterSampleStamp = ExitPositionSamples - 
                                                  QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(EntryOffset * SampleRate);
                    TransitionExitSampleStamp = ExitPositionSamples + Mathf.FloorToInt(ExitOffset * SampleRate);
                    break;
                default:
                    TransitionEnterSampleStamp = _transitionGridSampleStamp + GridLengthSamples - 
                                                  QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(EntryOffset * SampleRate);
                    TransitionExitSampleStamp = _transitionGridSampleStamp + GridLengthSamples 
                                                                            + Mathf.FloorToInt(ExitOffset * SampleRate);                
                    while (TransitionEnterSampleStamp < PlayHeadAudioSource.timeSamples)
                    {
                        TransitionEnterSampleStamp += GridLengthSamples;
                        TransitionExitSampleStamp += GridLengthSamples;
                    }             
                    break;
            }
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
            
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionEnter, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject, _currentTransitionEntryData.FadeInTime + "s fade in");
            CreateMusicTrackInstances(FadeInTime);            
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
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionExit, AudioTriggerSource.Code, CurrentPlayingEvent.name, gameObject, _currentTransitionExitData.FadeOutTime + "s fade out");
            foreach (var mti in _exitingTrackInstances)
            {
                mti.Stop(FadeOutTime);                
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
            TransitionEnterSampleStamp = ExitPositionSamples - QueuedMusicData.PickUpLengthSamples;
            TransitionExitSampleStamp = TrackLengthSamples;
        }
        
        private void SequenceEnter() //next sequence track plays
        {   
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SequenceEnter, AudioTriggerSource.Code, QueuedTracks[0].name, gameObject);
            ActiveTracks = QueuedTracks;
            QueuedTracks = new List<MusicTrack>();
            ActiveMusicData = QueuedMusicData;
            QueuedMusicData = null;
            
            CreateMusicTrackInstances();
            Invoke(nameof(SequenceExit), SamplesToTime(TransitionExitSampleStamp - TransitionEnterSampleStamp));
            TransitioningStatus = TransitioningStatus.Transitioning;
            PreEntry();
        }
        
        private void SequenceExit() //old sequence track finishes
        {
            foreach (var mti in _exitingTrackInstances)
            {
                mti.Stop(0f);                
            }

            _exitingTrackInstances = PlayingTrackInstances;
            if (TransitioningStatus != TransitioningStatus.PendingSequence) 
                TransitioningStatus = TransitioningStatus.None;
        }
        #endregion

        #region PlayHead	
        private int _beatSampleStamp;
        public BarAndBeat PlayHeadPosition;
        public AudioSource PlayHeadAudioSource;
        
        private void FixedUpdate()
        {
            if (!PlayHeadAudioSource || ActiveMusicData == null) return;

            //sometimes music is delayed one frame, fix it
            var adjustedSamples = CurrentSample + (int)(Time.fixedDeltaTime * SampleRate);
            
            //stinger queue time stamp is reached
            if (_triggerStingerSampleStamp > 0 && adjustedSamples > _triggerStingerSampleStamp) 
                PlayStinger();    
            
            if (adjustedSamples > _beatSampleStamp + BeatDurationSamples) 
                OnBeat();
            
            switch (TransitioningStatus)
            {
                case TransitioningStatus.Transitioning:
                    return;
                case TransitioningStatus.PendingSwitch:
                    if (adjustedSamples > _switchSampleStamp) 
                        SwitchCrossFade();
                    break;
                case TransitioningStatus.PendingTransition:
                    if (adjustedSamples > TransitionExitSampleStamp)
                    {                                     
                        TransitionExit();
                        return;
                    } 
                    if (adjustedSamples > TransitionEnterSampleStamp)                
                    {
                        TransitionEnter();
                        return;
                    } 
                    break;
                case TransitioningStatus.PendingSequence:
                    if (adjustedSamples > TransitionEnterSampleStamp)                
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
                    if (adjustedSamples > _transitionGridSampleStamp + GridLengthSamples)
                        OnTransitionGrid();
                    break;
            }

            if (UseDefaultLoopStyle)
            {
                //current sample is reset to 0, so it loops again
                if (PlayingStatus == PlayingStatus.Playing && adjustedSamples < _beatSampleStamp)
                    OnDefaultLoopStyleEndPosition();
            }
            else
            {
                switch (PlayingStatus)
                {
                    case PlayingStatus.Playing: //prepare to entry again
                        if (adjustedSamples > ExitPositionSamples - PickUpLengthSamples)
                            OnPreEntryAgainPosition();
                        break;
                    case PlayingStatus.PreEntry: //enter loop body
                    case PlayingStatus.PreEntryAgain:
                        if (adjustedSamples > PickUpLengthSamples)
                            OnLoopStartPosition();
                        break;
                    case PlayingStatus.PendingPostExit: //finish loop body                        
                        if (adjustedSamples > ExitPositionSamples)
                            OnPostExitPosition();
                        break;
                }
            }
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
        }
        
        public void Resume(float fadeInTime)
        {
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.Resume(fadeInTime);
            }
        }
        
        public void Mute(float fadeOutTime)
        {            
            foreach (var mti in PlayingTrackInstances)
            {			
                mti.Mute(fadeOutTime);
            }
        }
        
        public void UnMute(float fadeInTime)
        {            
            foreach (var mti in PlayingTrackInstances)
            {			                
                mti.UnMute(fadeInTime);                
            }
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