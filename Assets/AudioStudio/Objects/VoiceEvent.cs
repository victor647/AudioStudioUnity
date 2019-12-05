using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AudioStudio.Tools;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
	#region Enums	
	public enum VoicePlayLogic 
	{
		Single, 
		Random, 
		SequenceStep, 
		Switch		
	}
	#endregion
	
	[CreateAssetMenu(fileName = "New Voice Event", menuName = "Audio/Voice Event")]
	public class VoiceEvent : AudioEvent
	{							
		public VoicePlayLogic PlayLogic = VoicePlayLogic.Single;		
		public SwitchClipMapping[] SwitchClipMappings;					
		
		public AudioClip Clip;		
		public List<AudioClip> Clips = new List<AudioClip>();
		public int ClipCount;
		[NonSerialized]
		public List<VoiceEventInstance> VoiceEventInstances;				
		
		#region Initialize					
		public override void Init()
		{		
			LastSelectedIndex = 255;		
			VoiceEventInstances = new List<VoiceEventInstance>();            
        }

		public override void Dispose()
		{
			VoiceEventInstances.Clear();
		}
		#endregion
		
        #region Playback
        public override void PostEvent(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
        {	        
	        if (fadeInTime < 0) fadeInTime = DefaultFadeInTime;
	        Play(soundSource, fadeInTime, endCallback);
        }

        private AudioClip GetClip(GameObject soundSource)
		{
			switch (PlayLogic)
			{					
				case VoicePlayLogic.Random:
					if (Clips.Count < 2)
					{
						AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Voice, AudioAction.Play, AudioTriggerSource.Code, name, soundSource, "Random VoiceEvent only has 1 element");
						return Clips[0];
					}
					var selectedIndex = Random.Range(0, Clips.Count);
					if (!AvoidRepeat) return Clips[selectedIndex];
					while (selectedIndex == LastSelectedIndex)
					{
						selectedIndex = Random.Range(0, Clips.Count);
					}
					LastSelectedIndex = (byte)selectedIndex;
					return Clips[selectedIndex];					
				case VoicePlayLogic.Switch:
					var audioSwitch = AsAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
					if (audioSwitch)
					{
						var asi = audioSwitch.GetOrAddSwitchInstance(soundSource);
						foreach (var assignment in SwitchClipMappings)
						{
							if (assignment.SwitchName == asi.CurrentSwitch)
								return assignment.Clip;
						}
					}
					return SwitchClipMappings[0].Clip;
				case VoicePlayLogic.SequenceStep:
					LastSelectedIndex++;
					if (LastSelectedIndex == Clips.Count) LastSelectedIndex = 0;
					return Clips[LastSelectedIndex];		
			}
			return null;
		}

#if UNITY_EDITOR || !UNITY_WEBGL
		public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
		{
			if (PlayLogic != VoicePlayLogic.Single)
				Clip = GetClip(soundSource);
			if (!Clip)
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Voice, AudioAction.Play, AudioTriggerSource.Code, name, soundSource, "Audio Clip is missing!");
				return;
			}

			var vei = soundSource.AddComponent<VoiceEventInstance>();
			vei.Init(this, soundSource);
			vei.Play(fadeInTime);					
		}		
				
		public override void Stop(GameObject soundSource, float fadeOutTime)
		{			
			if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.StopEvent, AudioTriggerSource.Code, name, soundSource);									
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Stop(fadeOutTime);
			}				
		}
#else
		private WebGLStreamingAudioSourceInterop _interop;

		private int GetClipName()
		{
			switch (PlayLogic)
			{					
				case VoicePlayLogic.Random:					
					var selectedIndex = Random.Range(0, ClipCount);
					if (!AvoidRepeat) return selectedIndex;
					while (selectedIndex == LastSelectedIndex)
					{
						selectedIndex = Random.Range(0, ClipCount);
					}
					LastSelectedIndex = (byte)selectedIndex;
					return selectedIndex;					
				case VoicePlayLogic.SequenceStep:
					LastSelectedIndex++;
					if (LastSelectedIndex == ClipCount) LastSelectedIndex = 0;
					return LastSelectedIndex;		
			}
			return 0;
		}

		public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
		{
			var clipName = "";
			if (PlayLogic != VoicePlayLogic.Single)
				clipName = "Vo_" + name + "_" + (GetClipName() + 1).ToString("00");
			else
				clipName = "Vo_" + name;
			_interop = new WebGLStreamingAudioSourceInterop(AudioAssetLoader.GetClipUrl(clipName, ObjectType.Voice), soundSource);
			_interop.Play();
		}

		public override void Stop(GameObject soundSource, float fadeOutTime)
		{
			if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;
			_interop.Destroy();
		}	
#endif	
		#endregion

		#region Editor		
		public override void CleanUp()
		{			
			if (Platform == Platform.Web)
			{
				Clip = null;
				Clips = null;					
			}
			else
			{
				switch (PlayLogic)
				{
					case VoicePlayLogic.Single:
						if (!Clip)
							Debug.LogError("AudioClip of VoiceEvent " + name + " is missing!");
						break;
					case VoicePlayLogic.Random:
					case VoicePlayLogic.SequenceStep:
						if (Clips.Any(c => !c))
							Debug.LogError("AudioClips of VoiceEvent " + name + " is missing!");
						break;
					case VoicePlayLogic.Switch:
						if (SwitchClipMappings.Any( c=> !c.Clip))
							Debug.LogError("AudioClips of VoiceEvent " + name + " is missing!");
						break;
				}
			}

			if (PlayLogic != VoicePlayLogic.Switch)
			{
				SwitchEventMappings = null;
				SwitchClipMappings = null;
			}
		}
		
		public override bool IsValid()
		{
			return Clip != null || Clips.Any(c => c != null);
		}
		#endregion
	}

	public class VoiceEventInstance : AudioEventInstance
	{	
		#region Initialize
		public static int GlobalVoiceCount;
		
		private VoiceEvent _voiceEvent;
		
		public void Init(VoiceEvent evt, GameObject emitter)
		{			
			_voiceEvent = evt;
			Emitter = emitter;
			evt.VoiceEventInstances.Add(this);		
			
			AudioSource.clip = evt.Clip;			
			AudioSource.pitch = evt.Pitch;
			AudioSource.panStereo = evt.Pan;
			AudioSource.volume = evt.Volume;			
			AudioSource.outputAudioMixerGroup = AudioManager.GetAudioMixer("Voice", evt.AudioMixer);			
			
			if (evt.LowPassFilter)
			{
				LowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
				LowPassFilter.cutoffFrequency = evt.LowPassCutoff;
				LowPassFilter.lowpassResonanceQ = evt.LowPassResonance;
			}

			if (evt.HighPassFilter)
			{
				HighPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
				HighPassFilter.cutoffFrequency = evt.HighPassCutoff;
				HighPassFilter.highpassResonanceQ = evt.HighPassResonance;
			}

			foreach (var mapping in evt.Mappings)
			{
				mapping.Init(this, emitter);
			}
		}

		private void Awake()
		{
			AudioSource = gameObject.AddComponent<AudioSource>();
			GlobalVoiceCount++;
		}

		private void OnDestroy()
		{
			_voiceEvent.VoiceEventInstances.Remove(this);
			OnAudioEnd?.Invoke(Emitter);
			GlobalVoiceCount--;
			
			if (_voiceEvent.VoiceEventInstances.Count > 0) return;
			_voiceEvent.Dispose();
		}
		#endregion
		
		#region Playback				
		
		public void Play(float fadeInTime, Action<GameObject> endCallback = null)
		{
			OnAudioEnd = endCallback;
			StartCoroutine(AudioSource.Play(fadeInTime));	
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.Play, AudioTriggerSource.Code, AudioSource.clip.name, gameObject);
		}

		private void FixedUpdate()
		{
			if (AudioSource.timeSamples < TimeSamples && !AudioSource.loop)
			{
				AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.End, AudioTriggerSource.Code, AudioSource.clip.name, gameObject);
				AudioEnd();
			}
			TimeSamples = AudioSource.timeSamples;
		}
		
		#endregion
	}
}