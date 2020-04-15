using System;
using System.Collections.Generic;
using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioStudio.Configs
{
	public abstract class AudioEvent : AudioConfig
	{							
		#region Fields				
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
		public bool SubMixer;
		public string AudioMixer;
		public ParameterMapping[] Mappings = new ParameterMapping[0];						
		#endregion								

		#region Initialize
		protected List<AudioEventInstance> _playingInstances = new List<AudioEventInstance>(); 
		//When an event is loaded, before playing
		internal abstract void Init();
		//When an event is unloaded
		internal abstract void Dispose();
		#endregion
		
		#region Playback		
		public abstract string Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null);
		public abstract void Stop(GameObject soundSource, float fadeOutTime = 0f);
		internal abstract void StopAll(float fadeOutTime = 0f);
		internal abstract void Mute(GameObject soundSource, float fadeOutTime = 0f);
		internal abstract void UnMute(GameObject soundSource, float fadeInTime = 0f);
		internal abstract void Pause(GameObject soundSource, float fadeOutTime = 0f);
		internal abstract void Resume(GameObject soundSource, float fadeInTime = 0f);
		#endregion

		#region Editor
		internal abstract AudioObjectType GetEventType();
		#endregion
	}

	public abstract class AudioEventInstance : MonoBehaviour
	{
		internal AudioSource AudioSource;
		internal GameObject Emitter;
		protected Action<GameObject> OnAudioEnd;
		protected int TimeSamples = -1;
		protected PlayingStatus PlayingStatus = PlayingStatus.Idle;
		protected AudioLowPassFilter LowPassFilter;
		protected AudioHighPassFilter HighPassFilter;

		protected void OnAudioEndOrStop()
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

		internal void Stop(float fadeOutTime)
		{
			PlayingStatus = PlayingStatus.Stopping;
			TimeSamples = -1;
			if (isActiveAndEnabled && fadeOutTime > 0f)
				StartCoroutine(AudioSource.Stop(fadeOutTime, OnAudioEndOrStop));
			else
			{
				AudioSource.Stop();
				OnAudioEndOrStop();
			}
		}
		
		#region Controls
		internal virtual void SetOutputBus(AudioMixerGroup amg)
		{
			AudioSource.outputAudioMixerGroup = amg;
		}

		internal void Mute(float fadeOutTime)
		{
			if (isActiveAndEnabled && fadeOutTime > 0)
				StartCoroutine(AudioSource.Mute(fadeOutTime));
			else
				AudioSource.mute = true;
		}

		internal void UnMute(float fadeInTime)
		{
			if (isActiveAndEnabled && fadeInTime > 0)
				StartCoroutine(AudioSource.UnMute(fadeInTime));
			else
				AudioSource.mute = true;
		}

		internal void Pause(float fadeOutTime)
		{
			if (isActiveAndEnabled && fadeOutTime > 0)
				StartCoroutine(AudioSource.Pause(fadeOutTime));
			else
				AudioSource.Pause();
		}

		internal void Resume(float fadeInTime)
		{
			if (isActiveAndEnabled && fadeInTime > 0)
				StartCoroutine(AudioSource.Resume(fadeInTime));
			else
				AudioSource.UnPause();
		}

		internal virtual void SetVolume(float volume)
		{
			AudioSource.volume = volume;
		}

		internal virtual void SetPitch(float pitch)
		{
			AudioSource.pitch = pitch;
		}

		internal virtual void SetPan(float pan)
		{
			AudioSource.panStereo = pan;
		}

		internal void SetLowPassCutoff(float cutoff)
		{
			if (LowPassFilter) 
				LowPassFilter.cutoffFrequency = cutoff;
		}

		internal void SetHighPassCutoff(float cutoff)
		{
			if (HighPassFilter) 
				HighPassFilter.cutoffFrequency = cutoff;
		}
		#endregion
	}
}