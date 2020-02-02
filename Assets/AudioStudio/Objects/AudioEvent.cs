using System;
using AudioStudio.Components;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio.Configs
{
	public enum Platform
	{
		PC,
		Web
	}
	
	public abstract class AudioEvent : AudioConfig
	{							
		#region Settings				
		public Platform Platform;
		public bool IndependentEvent = true;
		public bool OverrideControls;
		public bool OverrideSpatial;
		
		//Audio Control Settings
		[Range(0f, 1f)]
		public float Volume = 1f;
		[Range(-4f, 4f)]
		public float Pitch = 1f;
		[Range(-1f, 1f)]
		public float Pan;
		public bool LowPassFilter;								
		public float LowPassResonance = 1f;
		[Range(10f, 22000f)]
		public float LowPassCutoff = 22000f;
		public bool HighPassFilter;	
		public float HighPassResonance = 1f;
		[Range(10f, 22000f)]
		public float HighPassCutoff = 10f;			
		
		//Audio Mixer Settings
		public bool SubMixer;
		public string AudioMixer;

		//For Random		
		protected byte LastSelectedIndex = 255;
		public bool AvoidRepeat = true;
		public bool RandomOnLoop;
		//For Switch			 
		public AudioSwitchReference AudioSwitchReference = new AudioSwitchReference();		
		public SwitchEventMapping[] SwitchEventMappings;
		public bool SwitchImmediately;
		public float CrossFadeTime = 0.5f;
		
		//For Parameter
		public ParameterMapping[] Mappings;						
		#endregion								

		//When an event is loaded, before playing
		public abstract void Init();						

		//When an event is unloaded
		public abstract void Dispose();
		
		#region Playback		
		public abstract void Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null);
		public abstract void Stop(GameObject soundSource, float fadeOutTime = 0f);
		public abstract void Mute(GameObject soundSource, float fadeOutTime = 0f);
		public abstract void UnMute(GameObject soundSource, float fadeInTime = 0f);
		public abstract void Pause(GameObject soundSource, float fadeOutTime = 0f);
		public abstract void Resume(GameObject soundSource, float fadeInTime = 0f);
		#endregion
	}

	public abstract class AudioEventInstance : MonoBehaviour
	{
		public AudioSource AudioSource;
		public GameObject Emitter;
		protected Action<GameObject> OnAudioEnd;
		protected int TimeSamples = -1;
		protected PlayingStatus PlayingStatus = PlayingStatus.Idle;
		protected AudioLowPassFilter LowPassFilter;
		protected AudioHighPassFilter HighPassFilter;

		private void OnDisable()
		{
			OnAudioEndOrStop();
		}

		protected virtual void OnAudioEndOrStop()
		{
			PlayingStatus = PlayingStatus.Idle;
			if (gameObject.name.EndsWith("(AudioSource)"))
			{
				var slave = gameObject.GetComponent<AudioTransformFollower>();
				if (slave)
					slave.AudioEnd(this);
			}
			else
				Destroy(this);
		}

		public virtual void Stop(float fadeOutTime)
		{
			PlayingStatus = PlayingStatus.Stopping;
			TimeSamples = -1;
			if (gameObject.activeInHierarchy && fadeOutTime > 0f)
				StartCoroutine(AudioSource.Stop(fadeOutTime, OnAudioEndOrStop));
			else
			{
				AudioSource.Stop();
				OnAudioEndOrStop();
			}
		}
		
		#region Controls
		public virtual void SetOutputBus(AudioMixerGroup amg)
		{
			AudioSource.outputAudioMixerGroup = amg;
		}

		public void Mute(float fadeOutTime)
		{
			StartCoroutine(AudioSource.Mute(fadeOutTime));
		}

		public void UnMute(float fadeInTime)
		{
			StartCoroutine(AudioSource.UnMute(fadeInTime));
		}
		
		public void Pause(float fadeOutTime)
		{
			StartCoroutine(AudioSource.Pause(fadeOutTime));
		}

		public void Resume(float fadeInTime)
		{
			StartCoroutine(AudioSource.Resume(fadeInTime));
		}

		public virtual void SetVolume(float volume)
		{
			AudioSource.volume = volume;
		}

		public virtual void SetPitch(float pitch)
		{
			AudioSource.pitch = pitch;
		}

		public virtual void SetPan(float pan)
		{
			AudioSource.panStereo = pan;
		}

		public void SetLowPassCutoff(float cutoff)
		{
			if (LowPassFilter) 
				LowPassFilter.cutoffFrequency = cutoff;
		}

		public void SetHighPassCutoff(float cutoff)
		{
			if (HighPassFilter) 
				HighPassFilter.cutoffFrequency = cutoff;
		}
		#endregion
	}
}