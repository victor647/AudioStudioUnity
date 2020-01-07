using System;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio.Configs
{
	[Flags]
	public enum MusicKey
	{
		None = 0,
		All = ~0,
		C = 0x1,
		Db = 0x2,
		D = 0x4,
		Eb = 0x8,
		E = 0x10,
		F = 0x20,
		Gb = 0x40,
		G = 0x80,
		Ab = 0x100,
		A = 0x200,
		Bb = 0x400,
		B = 0x800,
	}

	[Serializable]
	public class MusicMarker
	{
		public int BarNumber;
		public float Tempo = 120f;  
		public byte BeatsPerBar = 4;     
		public BeatDuration BeatDuration = BeatDuration._4;
		public MusicKey KeyCenter = MusicKey.C;
		
		public float BeatDurationRealtime()
		{
			if (BeatDuration == BeatDuration._4)
				return 60f / Tempo;
			if (BeatDuration == BeatDuration._8)
				return 30f / Tempo;
			return 15f / Tempo;
		}

		public float TotalDurationRealtime(float endBarNumber)
		{
			return BeatDurationRealtime() * BeatsPerBar * (endBarNumber - BarNumber);
		}
	}
	
	[CreateAssetMenu(fileName = "New Music Track", menuName = "AudioStudio/Music/Track")]
	public class MusicTrack : MusicContainer
	{				
		#region Editor
		public override void CleanUp()
		{
			if (Platform == Platform.Web) 
				Clip = null;
			else if (!Clip)
				Debug.LogError("AudioClip of MusicEvent " + name + " is missing!");

			ChildEvents = null;
			SwitchEventMappings = null;			
		}
		
		public override bool IsValid()
		{
			return Clip != null;
		}
		#endregion
		
		#region Settings
		public static int GlobalMusicCount;
		public MusicMarker[] Markers = new MusicMarker[1];
		public float PickupBeats;
		public BarAndBeat ExitPosition;
		public bool UseDefaultLoopStyle;		
		public AudioClip Clip;

		public virtual void OnPlay(){}
		#endregion
		
		#region Calculation

		public float LoopDurationRealTime()
		{
			if (UseDefaultLoopStyle && Clip)
				return Clip.length;
			
			var duration = 0f;
			for (var i = 0; i < Markers.Length - 1; i++)
			{
				duration += Markers[i].TotalDurationRealtime(Markers[i + 1].BarNumber);
			}
			duration += Markers[Markers.Length - 1].TotalDurationRealtime(ExitPosition.ToBars(Markers[Markers.Length - 1].BeatsPerBar));
			return duration;
		}
		
		#endregion
	}

	public class MusicTrackInstance : AudioEventInstance
	{        
		#region Initialize
		public MusicTrack MusicTrack;
		private float _volume;

		private void Awake()
		{
			AudioSource = _source1 = gameObject.AddComponent<AudioSource>();
			Emitter = GlobalAudioEmitter.GameObject;
			MusicTrack.GlobalMusicCount++;
		}

		public void Init(MusicTrack track)
		{
			MusicTrack = track;
			_volume = track.Volume;
			MusicTransport.Instance.PlayingTrackInstances.Add(this);
			track.Clip.LoadAudioData();
			_source1.clip = track.Clip;			
			_source1.outputAudioMixerGroup = AudioManager.GetAudioMixer("Music", track.AudioMixer);
			
			if (track.UseDefaultLoopStyle)
				AudioSource.loop = true;
			else
			{
				_source2 = gameObject.AddComponent<AudioSource>();
				_source2.clip = track.Clip;
				_source2.outputAudioMixerGroup = AudioManager.GetAudioMixer("Music", track.AudioMixer);			
			}
			
			if (track.LowPassFilter)
			{
				LowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
				LowPassFilter.cutoffFrequency = MusicTrack.LowPassCutoff;
				LowPassFilter.lowpassResonanceQ = MusicTrack.LowPassResonance;
			}
			if (track.HighPassFilter)
			{
				HighPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
				HighPassFilter.cutoffFrequency = MusicTrack.HighPassCutoff;
				HighPassFilter.highpassResonanceQ = MusicTrack.HighPassResonance;
			}

			foreach (var mapping in track.Mappings)
			{
				mapping.Init(this, Emitter);									
			}					
		}
        
        private void OnDestroy()
        {
	        foreach (var mapping in MusicTrack.Mappings)
	        {
		        mapping.Dispose(Emitter);
	        }
	        
	        Destroy(_source1);
	        if (!MusicTrack.UseDefaultLoopStyle)
				Destroy(_source2);
	        OnAudioEnd?.Invoke(Emitter);
	        MusicTrack.Dispose();
	        MusicTrack.GlobalMusicCount--;
        }
		#endregion
		
		#region Playback
		private AudioSource _source1;
		private AudioSource _source2;				
		
		public void Play(float fadeInTime, int timeSamples = 0)
		{						
			if (MusicTrack.UseDefaultLoopStyle)
			{				
				if (AudioSource.isPlaying) return;											
			}
			else											
				AudioSource = AudioSource == _source1 ? _source2 : _source1;		
			
			AudioSource.volume = _volume;
			AudioSource.panStereo = MusicTrack.Pan;
			AudioSource.pitch = MusicTrack.Pitch;
			AudioSource.timeSamples = timeSamples > MusicTrack.Clip.samples? 0 : timeSamples;
			StartCoroutine(AudioSource.Play(fadeInTime));
		}

		public override void Stop(float fadeOutTime)
		{
			StartCoroutine(AudioSource.Stop(fadeOutTime, AudioEnd));
		}
		#endregion

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