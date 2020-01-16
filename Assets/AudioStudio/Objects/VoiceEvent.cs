using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AudioStudio.Components;
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
	
	[CreateAssetMenu(fileName = "New Voice Event", menuName = "AudioStudio/Voice Event")]
	public class VoiceEvent : AudioEvent
	{						
		#region Fields
		public static int GlobalVoiceCount;
		public VoicePlayLogic PlayLogic = VoicePlayLogic.Single;		
		public SwitchClipMapping[] SwitchClipMappings;					
		
		public AudioClip Clip;		
		public List<AudioClip> Clips = new List<AudioClip>();
		public int ClipCount;
		[NonSerialized]
		public List<VoiceEventInstance> VoiceEventInstances;	
		#endregion
		
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
        private AudioClip GetClip(GameObject soundSource)
		{
			switch (PlayLogic)
			{					
				case VoicePlayLogic.Random:
					if (Clips.Count < 2)
						return Clips[0];
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
		public override void Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
		{
			if (PlayLogic != VoicePlayLogic.Single)
				Clip = GetClip(soundSource);
			if (!Clip) return;
			var vei = soundSource.AddComponent<VoiceEventInstance>();
			vei.Init(this, soundSource);
			vei.Play(fadeInTime);					
		}		
				
		public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
		{
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

		public override void Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
		{
			var clipName = "";
			if (PlayLogic != VoicePlayLogic.Single)
				clipName = "Vo_" + name + "_" + (GetClipName() + 1).ToString("00");
			else
				clipName = "Vo_" + name;
			_interop = new WebGLStreamingAudioSourceInterop(AudioAssetLoader.GetClipUrl(clipName, ObjectType.Voice), soundSource);
			_interop.Play();
		}

		public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
		{
			_interop.Destroy();
		}	
#endif	
		
		public override void Mute(GameObject soundSource, float fadeOutTime = 0f)
		{
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Mute(fadeOutTime);
			}
		}

		public override void UnMute(GameObject soundSource, float fadeInTime = 0f)
		{
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.UnMute(fadeInTime);
			}
		}

		public override void Pause(GameObject soundSource, float fadeOutTime = 0f)
		{
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Pause(fadeOutTime);
			}
		}

		public override void Resume(GameObject soundSource, float fadeInTime = 0f)
		{
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Resume(fadeInTime);
			}
		}
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
			VoiceEvent.GlobalVoiceCount++;
		}

		private void OnDestroy()
		{
			_voiceEvent.VoiceEventInstances.Remove(this);
			OnAudioEnd?.Invoke(Emitter);
			Destroy(AudioSource);
			VoiceEvent.GlobalVoiceCount--;
			
			if (_voiceEvent.VoiceEventInstances.Count > 0) return;
			_voiceEvent.Dispose();
		}
		#endregion
		
		#region Playback				
		
		public void Play(float fadeInTime, Action<GameObject> endCallback = null)
		{
			OnAudioEnd = endCallback;
			StartCoroutine(AudioSource.Play(fadeInTime));
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