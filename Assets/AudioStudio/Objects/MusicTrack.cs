using System;
using AudioStudio.Components;
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

		public float BeatDurationInSeconds()
		{
			if (BeatDuration == BeatDuration._4)
				return 60f / Tempo;
			if (BeatDuration == BeatDuration._8)
				return 30f / Tempo;
			return 15f / Tempo;
		}
		
		public float BarDurationInSeconds => BeatDurationInSeconds() * BeatsPerBar;

		public float TotalDurationInSeconds(float endBarNumber)
		{
			return BeatDurationInSeconds() * BeatsPerBar * (endBarNumber - BarNumber);
		}
	}
	
	[CreateAssetMenu(fileName = "New Music Track", menuName = "AudioStudio/Music/Track")]
	public class MusicTrack : MusicContainer
	{				
		#region Editor
		public override void CleanUp()
		{
			ChildEvents.Clear();
			if (!Clip)
				Debug.LogError("AudioClip of MusicEvent " + name + " is missing!");
		}
		
		public override bool IsValid()
		{
			return Clip != null;
		}
		#endregion
		
		#region Initialize

		internal override void Init()
		{
			Clip.LoadAudioData();									                   
		}

		internal override void Dispose()
		{
			Clip.UnloadAudioData();										            
		}
		
		internal void AddInstance(MusicTrackInstance instance)
		{
			EmitterManager.AddMusicInstance(instance);
		}

		internal void RemoveInstance(MusicTrackInstance instance)
		{
			EmitterManager.RemoveMusicInstance(instance);
		}
		#endregion

		#region Settings
		public MusicMarker[] Markers = new MusicMarker[1];
		public float PickupBeats;
		public BarAndBeat ExitPosition;
		public bool UseDefaultLoopStyle;		
		public AudioClip Clip;
		public void OnPlay(){}
		#endregion
		
		#region Calculation
		public int LoopDurationSamples()
		{
			if (UseDefaultLoopStyle)
				return Clip.samples;
			var duration = 0f;
			for (var i = 0; i < Markers.Length - 1; i++)
			{
				duration += Markers[i].TotalDurationInSeconds(Markers[i + 1].BarNumber);
			}
			duration += Markers[Markers.Length - 1].TotalDurationInSeconds(ExitPosition.ToBars(Markers[Markers.Length - 1].BeatsPerBar));
			return Mathf.FloorToInt(duration * Clip.frequency);
		}
		#endregion
	}

	public class MusicTrackInstance : AudioEventInstance
	{        
		#region Initialize
		internal MusicTrack MusicTrack;
		private float _volume;

		public void Init(MusicTrack track)
		{
			AudioSource = _source1 = gameObject.AddComponent<AudioSource>();
			Emitter = GlobalAudioEmitter.GameObject;
			
			MusicTrack = track;
			_volume = track.Volume;
			
			MusicTrack.AddInstance(this);
			InitAudioSource(_source1);

			if (track.LoopCount != 1)
			{
				if (track.UseDefaultLoopStyle)
					AudioSource.loop = true;
				else
				{
					_source2 = gameObject.AddComponent<AudioSource>();
					InitAudioSource(_source2);
					_source2.playOnAwake = false;
				}
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

		private void InitAudioSource(AudioSource source)
		{
			source.clip = MusicTrack.Clip;
			source.outputAudioMixerGroup = AudioManager.GetAudioMixer("Music", MusicTrack.AudioMixer);
		}

		private void OnDestroy()
        {
	        foreach (var mapping in MusicTrack.Mappings)
	        {
		        mapping.Dispose(Emitter);
	        }
	        
	        Destroy(_source1);
	        if (_source2)
				Destroy(_source2);
	        OnAudioEnd?.Invoke(Emitter);
	        MusicTrack.RemoveInstance(this);
        }
		#endregion
		
		#region Playback
		private AudioSource _source1;
		private AudioSource _source2;				
		
		public void Play(float fadeInTime, int timeSamples = 0)
		{						
			if (!_source2)
			{				
				if (AudioSource.isPlaying) return;											
			}
			else
			{
				AudioSource.playOnAwake = false;
				AudioSource = AudioSource == _source1 ? _source2 : _source1;
				AudioSource.playOnAwake = true;
			}

			AudioSource.volume = _volume;
			AudioSource.panStereo = MusicTrack.Pan;
			AudioSource.pitch = MusicTrack.Pitch;
			AudioSource.timeSamples = timeSamples > MusicTrack.Clip.samples? 0 : timeSamples;
			if (isActiveAndEnabled && fadeInTime > 0f)
				StartCoroutine(AudioSource.Play(fadeInTime));
			else
				AudioSource.Play();
		}
		#endregion

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