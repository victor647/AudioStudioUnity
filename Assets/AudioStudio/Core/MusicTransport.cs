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
        Transitioning,
    }

    public enum SequencingStatus
    {
        None,
        PendingSequence,
        ChangingSequence
    }

    public enum SwitchingStatus
    {
        None,
        PendingSwitch,
        SwitchCrossFading
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
        public int PickUpLengthSamples => MusicTrack.UseDefaultLoopStyle ? 0 : Mathf.FloorToInt(MusicTrack.PickupBeats * Markers[0].BeatDurationInSeconds() * SampleRate);
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
                if (!_instance)
                {
                    var go = new GameObject("Music Transport");
                    _instance = go.AddComponent<MusicTransport>();
                    DontDestroyOnLoad(_instance);
                }
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
        private int SampleRate
        {
            get
            {
                if (ActiveMusicData != null)
                    return ActiveMusicData.SampleRate;
                Debug.LogError("AudioStudio: Can't find Active Music Data!");
                return 44100;
            }
        }

        public int CurrentSample => PlayHeadAudioSource ? PlayHeadAudioSource.timeSamples : 0;
        public MusicMarker CurrentMarker => ActiveMusicData.CurrentMarker;
        public BarAndBeat ExitPosition => ActiveMusicData.MusicTrack.ExitPosition;
        private int BeatsPerBar => ActiveMusicData.CurrentMarker.BeatsPerBar;
        private int BeatDurationSamples => Mathf.FloorToInt(CurrentMarker.BeatDurationInSeconds() * SampleRate);
        private int PickUpLengthSamples => ActiveMusicData.PickUpLengthSamples;
        private int ExitPositionSamples => ActiveMusicData.MusicTrack.LoopDurationSamples() + PickUpLengthSamples;
        private int TrackLengthSamples => ActiveMusicData.MusicTrack.Clip.samples;
        #endregion
        
        #region PlayEvent
        public TransportData ActiveMusicData, QueuedMusicData;
        public MusicContainer CurrentEvent, QueuedEvent;
        public List<MusicTrack> ActiveTracks = new List<MusicTrack>(); 
        public List<MusicTrack> QueuedTracks = new List<MusicTrack>();
        private List<MusicTrackInstance> _playingInstances = new List<MusicTrackInstance>();
        private List<MusicTrackInstance> _exitingInstances = new List<MusicTrackInstance>();

        public string SetMusicQueue(MusicContainer newMusic, float fadeInTime = 0f)
        {
            // prevent duplicate event calling
            if (QueuedEvent == newMusic)
                return QueuedTracks[0].name;
            
            _currentTransitionExitData = GetTransitionExitCondition(newMusic);
            _currentTransitionEntryData = GetTransitionEntryCondition(newMusic);

            var selectedTrack = string.Empty;
            // directly play the music if no music is playing
            if (!CurrentEvent) 
            {
                CurrentEvent = newMusic;
                ActiveTracks.Clear();
                GetTracks(newMusic, ActiveTracks);
                ActiveMusicData = new TransportData(ActiveTracks[0]);
                selectedTrack = ActiveTracks[0].name;
                Play(fadeInTime);                                
            }
            else if (CurrentEvent != newMusic)
            {
                // stop any pending transition, switch or sequence
                CancelTransition();
                CancelSwitch();
                CancelSequence();
                
                QueuedEvent = newMusic;
                QueuedTracks.Clear();
                GetTracks(newMusic, QueuedTracks);
                QueuedMusicData = new TransportData(QueuedTracks[0]);
                selectedTrack = QueuedTracks[0].name;
                // calculate the time for transition
                PrepareTransition();
            }
            _exitingInstances = _playingInstances;
            return selectedTrack;
        }

        private static void GetTracks(MusicContainer evt, ICollection<MusicTrack> trackList)
        {
            var track = evt as MusicTrack;
            // if this is single track just get it
            if (track) 
            {
                trackList.Add(track);
                return;
            }
            // play all layered tracks together
            if (evt is MusicLayerContainer) 
            {
                foreach (var childEvent in evt.ChildEvents)
                {
                    GetTracks(childEvent, trackList);
                }
            }
            // choose a track by play logic
            else 
            {
                var musicContainer = evt.GetEvent();
                GetTracks(musicContainer, trackList);
            }
        }
        
        private void CreatePlayingInstances(float fadeInTime = 0f, int timeSamples = 0)
        {            
            if (ActiveTracks.Count == 0) return;
            _playingInstances = new List<MusicTrackInstance>();
            // generate the audio sources
            foreach (var track in ActiveTracks)
            {
                var mti = gameObject.AddComponent<MusicTrackInstance>();                     
                mti.Init(track);
                _playingInstances.Add(mti);
                mti.Play(fadeInTime, timeSamples);                    
            }
        }
        #endregion

        #region PlayingStatus
        public PlayingStatus PlayingStatus = PlayingStatus.Idle;
        public Action PlayCallback, LoopCallback, ExitCallback, StopCallback, BeatCallback, BarCallback;

        public void Play(float fadeInTime)
        {                      
            CreatePlayingInstances(fadeInTime);              
            PreEntry();        
            PlayCallback?.Invoke();            
        }

        private void PreEntry()
        {
            _beatSample = 0;
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
            _beatSample = _transitionGridSample = PickUpLengthSamples;
            foreach (var track in ActiveTracks)
            {
                track.OnPlay();
            }
            LoopCallback?.Invoke();
            
            if (RemainingLoops != 1) //more loops to go
                AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Loop, AudioTriggerSource.Code, ActiveTracks[0].name, gameObject);
            else
            {
                var seqeunceContainer = CurrentEvent as MusicSequenceContainer;
                if (seqeunceContainer && !seqeunceContainer.SequenceByStep)            
                    SetSequence(seqeunceContainer.GetNextEvent());
            }            
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
            _beatSample = 0;
            foreach (var msi in _playingInstances)
            {
                msi.Play(0); //play from the top again
            }
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
            foreach (var instance in _playingInstances)
            {
                if (instance) instance.Stop(fadeOutTime);                                
            }
            foreach (var instance in _exitingInstances)
            {
                if (instance) instance.Stop(fadeOutTime);                
            }
            Invoke(nameof(ResetAll), fadeOutTime);  
        }

        private void ResetAll() //reset music transport to idle
        {            
            PlayingStatus = PlayingStatus.Idle;
            CancelTransition();
            CancelSwitch();
            CancelSequence();
            
            PlayHeadPosition = BarAndBeat.Zero;            
            CurrentEvent = null;
            QueuedEvent = null;
            _playingInstances.Clear();  
            _exitingInstances.Clear();
            ActiveMusicData = null;
            QueuedMusicData = null;
            StopCallback?.Invoke();
        }
        #endregion

        #region Transition       
        public TransitioningStatus TransitioningStatus = TransitioningStatus.None;
        private TransitionEntryData _currentTransitionEntryData;
        private TransitionExitData _currentTransitionExitData;
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
        public int TransitionExitSample;
        public int TransitionEnterSample;
        private int _transitionGridSample;
        private bool _exitFirst = true;
        
        private TransitionExitData GetTransitionExitCondition(MusicContainer newMusic)
        {
            if (!CurrentEvent)
                return new TransitionExitData();
            return CurrentEvent.TransitionExitConditions.FirstOrDefault(c => c.Target.Name == newMusic.name) ?? 
                   CurrentEvent.TransitionExitConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Target.Name)) ??
                   new TransitionExitData();
        }
        
        private TransitionEntryData GetTransitionEntryCondition(MusicContainer newMusic)
        {
            if (!CurrentEvent)
                return newMusic.TransitionEntryConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Source.Name)) ?? new TransitionEntryData();
            return newMusic.TransitionEntryConditions.FirstOrDefault(c => c.Source.Name == CurrentEvent.name) ?? 
                   newMusic.TransitionEntryConditions.FirstOrDefault(c => string.IsNullOrEmpty(c.Source.Name)) ??
                   new TransitionEntryData();
        }
        
        private void PrepareTransition()
        {
            // check if there are any segments connecting
            if (!string.IsNullOrEmpty(_currentTransitionEntryData.TransitionSegment.Name))
            {
                var nextEvent = QueuedEvent;
                var segment = AsAssetLoader.GetAudioEvent(_currentTransitionEntryData.TransitionSegment.Name) as MusicTrack;
                if (segment)
                {
                    SetMusicQueue(segment);
                    SetSequence(nextEvent);
                    return;
                }
            }
            // calculate the sample of transition exit and enter
            switch (TransitionInterval)
            {
                case TransitionInterval.Immediate:
                    TransitionEnterSample = CurrentSample + Mathf.Max(0, Mathf.FloorToInt(EntryOffset * SampleRate));
                    TransitionExitSample = CurrentSample + Mathf.Max(0, Mathf.FloorToInt(ExitOffset * SampleRate));
                    break;
                case TransitionInterval.ExitCue:
                    TransitionEnterSample = ExitPositionSamples - QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(EntryOffset * SampleRate);
                    TransitionExitSample = ExitPositionSamples + Mathf.FloorToInt(ExitOffset * SampleRate);
                    break;
                default:
                    TransitionEnterSample = _transitionGridSample + GridLengthSamples - QueuedMusicData.PickUpLengthSamples + Mathf.FloorToInt(EntryOffset * SampleRate);
                    TransitionExitSample = _transitionGridSample + GridLengthSamples 
                                                                            + Mathf.FloorToInt(ExitOffset * SampleRate);                
                    while (TransitionEnterSample < CurrentSample)
                    {
                        TransitionEnterSample += GridLengthSamples;
                        TransitionExitSample += GridLengthSamples;
                    }             
                    break;
            }
            // determine if new music plays first or old music stops first
            _exitFirst = TransitionExitSample <= TransitionEnterSample;
            // set transition status
            TransitioningStatus = TransitioningStatus.PendingTransition;
        }

        private void CancelTransition()
        {
            TransitioningStatus = TransitioningStatus.None;
            CancelInvoke(nameof(TransitionEnter));
            CancelInvoke(nameof(TransitionExit));
        }

        // new music starts playing
        private void TransitionEnter() 
        {            
            if (QueuedMusicData == null) return;
            // replace current tracks with queued tracks
            CurrentEvent = QueuedEvent;
            QueuedEvent = null;
            ActiveTracks = QueuedTracks;
            QueuedTracks = new List<MusicTrack>();
            ActiveMusicData = QueuedMusicData;
            QueuedMusicData = null;
            
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionEnter, AudioTriggerSource.Code, CurrentEvent.name, gameObject, _currentTransitionEntryData.FadeInTime + "s fade in");
            CreatePlayingInstances(FadeInTime);            
            PreEntry();
            // call transition exit if new music should play before old music stops
            if (!_exitFirst)
            {
                TransitioningStatus = TransitioningStatus.Transitioning;
                Invoke(nameof(TransitionExit), SamplesToTime(TransitionExitSample - TransitionEnterSample));
            }
            // transition process has finished
            else if (TransitioningStatus == TransitioningStatus.Transitioning)           
                TransitioningStatus = TransitioningStatus.None;
        }

        private void TransitionExit() //old music finishes playing
        {                        
            if (ActiveMusicData == null) return;
            foreach (var mti in _exitingInstances)
            {
                mti.Stop(FadeOutTime);                
            }
            _exitingInstances.Clear();    
            // call transition enter if new music should stop before new music plays
            if (_exitFirst)
            {
                TransitioningStatus = TransitioningStatus.Transitioning;
                Invoke(nameof(TransitionEnter), SamplesToTime(TransitionEnterSample - TransitionExitSample));
            }
            else
                Invoke(nameof(OnTransitionEnd), FadeOutTime);
        }
        
        private void OnTransitionEnd()
        {
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.TransitionExit, AudioTriggerSource.Code, CurrentEvent.name, gameObject, _currentTransitionExitData.FadeOutTime + "s fade out");
            if (TransitioningStatus == TransitioningStatus.Transitioning)
                TransitioningStatus = TransitioningStatus.None;
        }
        #endregion
        
        #region Switch
        public SwitchingStatus SwitchingStatus = SwitchingStatus.None;
        private bool _switchToSamePosition;
        private float _crossFadeTime;
        public int SwitchEnterSample;
        
        public void OnSwitchChanged(bool switchImmediately, bool toSamePosition, float crossFadeTime) //audio switch is set to a different value that affects music
        {
            _crossFadeTime = crossFadeTime;
            _switchToSamePosition = toSamePosition;
            // wait for transition grid to switch
            if (switchImmediately || TransitionInterval == TransitionInterval.Immediate)
                SwitchCrossFadeStart();
            else
            {
                switch (TransitionInterval)
                {
                    case TransitionInterval.ExitCue:
                        SwitchEnterSample = ExitPositionSamples - PickUpLengthSamples - TimeToSamples(crossFadeTime / 2f);
                        SwitchingStatus = SwitchingStatus.PendingSwitch;
                        break;
                    default:
                        SwitchEnterSample = _transitionGridSample + GridLengthSamples - TimeToSamples(crossFadeTime / 2f);
                        SwitchingStatus = SwitchingStatus.PendingSwitch;
                        break;
                }
            }
        }
        
        private void SwitchCrossFadeStart()
        {
            SwitchingStatus = SwitchingStatus.SwitchCrossFading;
            // fade out the old music
            foreach (var msi in _playingInstances)
            {
                msi.Stop(_crossFadeTime);                
            }          
            // reset active tracks
            ActiveTracks.Clear();            
            GetTracks(CurrentEvent, ActiveTracks);
            ActiveMusicData = new TransportData(ActiveTracks[0]);
            // play the new music at the same position
            if (_switchToSamePosition && PlayHeadAudioSource)
                CreatePlayingInstances(_crossFadeTime, PlayHeadAudioSource.timeSamples);
            // play the new music from the beginning
            else
            {
                CreatePlayingInstances(_crossFadeTime);
                PreEntry();
            }       
            Invoke(nameof(CancelSwitch), _crossFadeTime);
        }

        private void CancelSwitch()
        {
            // reset switch status
            SwitchingStatus = SwitchingStatus.None;
        }
        #endregion
        
        #region SequenceContinuous

        public SequencingStatus SequencingStatus = SequencingStatus.None;
        public int SequenceEnterSample;
        public int SequenceExitSample;

        private void SetSequence(MusicContainer nextMusic) //get the next item of a sequence continuous music container
        {
            // return if reaches the last item in sequence
            if (!nextMusic) return;             
            SequencingStatus = SequencingStatus.PendingSequence;
            // get the list of queued sequence tracks
            QueuedTracks.Clear();
            GetTracks(nextMusic, QueuedTracks);
            QueuedMusicData = new TransportData(QueuedTracks[0]);
            
            SequenceEnterSample = ExitPositionSamples - QueuedMusicData.PickUpLengthSamples;
            SequenceExitSample = TrackLengthSamples;
        }
        
        private void SequenceEnter() //next sequence track plays
        {   
            if (QueuedMusicData == null) return;
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SequenceEnter, AudioTriggerSource.Code, QueuedTracks[0].name, gameObject);
            ActiveTracks = QueuedTracks;
            QueuedTracks = new List<MusicTrack>();
            ActiveMusicData = QueuedMusicData;
            QueuedMusicData = null;
            _exitingInstances = _playingInstances;
            
            CreatePlayingInstances();
            PreEntry();
            
            // if the new track has pre-entry, old track should exit later
            var waitTime = SamplesToTime(SequenceExitSample - SequenceEnterSample);
            if (waitTime <= 0f)
                SequenceExit();
            else
            {
                SequencingStatus = SequencingStatus.ChangingSequence;
                Invoke(nameof(SequenceExit), waitTime);
            }
        }
        
        private void SequenceExit() //old sequence track finishes
        {
            if (ActiveMusicData == null) return;
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SequenceExit, AudioTriggerSource.Code, _exitingInstances[0].MusicTrack.name, gameObject);
            // stop the old tracks
            foreach (var mti in _exitingInstances)
            {
                mti.Stop(0f);                
            }
            // if no more sequenced tracks, reset transition status
            if (SequencingStatus != SequencingStatus.PendingSequence) 
                SequencingStatus = SequencingStatus.None;
        }
        
        private void CancelSequence()
        {
            SequencingStatus = SequencingStatus.None;
            CancelInvoke(nameof(SequenceExit));
        }
        #endregion
        
        #region Stinger
        private MusicStinger _queuedStinger;
        private int _triggerStingerSample;
        public MusicKey CurrentKey => CurrentMarker.KeyCenter;
        
        public void QueueStinger(MusicStinger stinger)
        {            
            _queuedStinger = stinger;
            // determine when the stinger will be played
            switch (stinger.TriggerSync)
            {
                case TransitionInterval.Immediate:
                    PlayStinger();
                    break;
                case TransitionInterval.NextBar:
                    _triggerStingerSample = Mathf.FloorToInt(_beatSample + BeatsPerBar * BeatDurationSamples - stinger.PickUpLength * SampleRate);                                         
                    while (CurrentSample >= _triggerStingerSample)
                    {
                        _triggerStingerSample += BeatsPerBar * BeatDurationSamples;
                    }
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SetQueue, AudioTriggerSource.Code, stinger.name, gameObject, "Stinger will play next bar");
                    break;
                case TransitionInterval.NextBeat:
                    _triggerStingerSample = Mathf.FloorToInt(_beatSample + BeatDurationSamples - stinger.PickUpLength * SampleRate);                                            
                    while (CurrentSample >= _triggerStingerSample)
                    {
                        _triggerStingerSample += BeatDurationSamples;
                    }
                    AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.SetQueue, AudioTriggerSource.Code, stinger.name, gameObject, "Stinger will play next beat");
                    break;
            }            
        }
        
        private void PlayStinger()
        {
            _triggerStingerSample = 0;
            if (_playingInstances.Count == 0)
            {
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Music, AudioAction.Stinger, AudioTriggerSource.Code, _queuedStinger.name, gameObject, "Stinger won't play without music");
                return;
            }
            // find stinger for current key
            foreach (var keyAssignment in _queuedStinger.KeyAssignments)
            {
                if ((keyAssignment.Keys & CurrentKey) != MusicKey.None)
                    PlayHeadAudioSource.PlayOneShot(keyAssignment.Clip, _queuedStinger.Volume);       
            }
            AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Music, AudioAction.Stinger, AudioTriggerSource.Code, _queuedStinger.name, gameObject, CurrentKey.ToString());
        }
        #endregion

        #region PlayHead	
        private int _beatSample;
        public BarAndBeat PlayHeadPosition;
        public AudioSource PlayHeadAudioSource
        {
            get
            {
                if (_playingInstances.Count > 0)
                    return _playingInstances[0].AudioSource;
                if (_exitingInstances.Count > 0)
                    return _exitingInstances[0].AudioSource;
                return null;
            }
        }

        internal void UpdateMusicPlayback()
        {
            // make sure music is playing
            if (PlayingStatus == PlayingStatus.Idle || PlayingStatus == PlayingStatus.Stopping) return;
            // time of a beat has passed
            if (CurrentSample > _beatSample + BeatDurationSamples) 
                OnBeat();
            //checking for exit or loop point
            switch (PlayingStatus)
            {
                case PlayingStatus.Playing:
                case PlayingStatus.PreEntryAgain:
                case PlayingStatus.PendingPostExit:
                    if (CurrentSample > _transitionGridSample + GridLengthSamples)
                        OnTransitionGrid();
                    break;
            }
            // stinger queue time stamp is reached
            if (_triggerStingerSample > 0 && CurrentSample > _triggerStingerSample) 
                PlayStinger();  
            // check if transition will happen
            if (TransitioningStatus == TransitioningStatus.PendingTransition)
            {
                if (_exitFirst && CurrentSample > TransitionExitSample)
                    TransitionExit();
                if (!_exitFirst && CurrentSample > TransitionEnterSample)
                    TransitionEnter();
            }
            // do not check other logic in a transition
            if (TransitioningStatus == TransitioningStatus.Transitioning)
                return;
            // check if switch will happen
            if (SwitchingStatus == SwitchingStatus.PendingSwitch && CurrentSample > SwitchEnterSample)
                SwitchCrossFadeStart();
            // check if sequence will change
            if (SequencingStatus == SequencingStatus.PendingSequence && CurrentSample > SequenceEnterSample)
                SequenceEnter();

            if (UseDefaultLoopStyle)
            {
                //current sample is reset to 0, so it loops again
                if (PlayingStatus == PlayingStatus.Playing && CurrentSample < _beatSample)
                    OnDefaultLoopStyleEndPosition();
            }
            else
            {
                switch (PlayingStatus)
                {
                    //prepare to entry again
                    case PlayingStatus.Playing:
                        if (CurrentSample > ExitPositionSamples - PickUpLengthSamples)
                            OnPreEntryAgainPosition();
                        break;
                    //enter loop body
                    case PlayingStatus.PreEntry: 
                    case PlayingStatus.PreEntryAgain:
                        if (CurrentSample > PickUpLengthSamples)
                            OnLoopStartPosition();
                        break;
                    //finish loop body          
                    case PlayingStatus.PendingPostExit:               
                        if (CurrentSample > ExitPositionSamples)
                            OnPostExitPosition();
                        break;
                }
            }
        }
        
        private void OnBeat()
        {
            _beatSample += BeatDurationSamples;
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
            _transitionGridSample += GridLengthSamples;
        }

        private float SamplesToTime(int samples)
        {
            return samples / Mathf.Abs(CurrentEvent.Pitch) / SampleRate;
        }
        
        private int TimeToSamples(float time)
        {
            return Mathf.FloorToInt(time * Mathf.Abs(CurrentEvent.Pitch) * SampleRate);
        }
        #endregion

        #region Controls
        public void Pause(float fadeOutTime)
        {
            foreach (var mti in _playingInstances)
            {			
                mti.Pause(fadeOutTime);
            }
        }
        
        public void Resume(float fadeInTime)
        {
            foreach (var mti in _playingInstances)
            {			
                mti.Resume(fadeInTime);
            }
        }
        
        public void Mute(float fadeOutTime)
        {            
            foreach (var mti in _playingInstances)
            {			
                mti.Mute(fadeOutTime);
            }
        }
        
        public void UnMute(float fadeInTime)
        {            
            foreach (var mti in _playingInstances)
            {			                
                mti.UnMute(fadeInTime);                
            }
        }
        
        public void SetOutputBus(AudioMixerGroup bus)
        {
            foreach (var mti in _playingInstances)
            {			
                mti.SetOutputBus(bus);
            }
        }
                
        public void SetVolume(float volume)
        {            
            volume = Mathf.Clamp01(volume);
            foreach (var mti in _playingInstances)
            {			
                mti.SetVolume(volume);
            }
        }
        
        public void SetPan(float pan)
        {            
            pan = Mathf.Clamp(pan, -1f, 1f);
            foreach (var mti in _playingInstances)
            {			
                mti.SetPan(pan);
            }
        }
                
        public void SetLowPassCutoff(float cutoff)
        {            
            cutoff = Mathf.Clamp(cutoff, 10f, 22000f);
            foreach (var mti in _playingInstances)
            {			
                mti.SetLowPassCutoff(cutoff);
            }
        }
                
        public void SetHighPassCutoff(float cutoff)
        {            
            cutoff = Mathf.Clamp(cutoff, 10f, 22000f);
            foreach (var mti in _playingInstances)
            {			
                mti.SetHighPassCutoff(cutoff);
            }
        }
        #endregion
    }   
}