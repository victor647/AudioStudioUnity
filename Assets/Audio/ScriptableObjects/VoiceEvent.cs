using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Linq;
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
	public partial class VoiceEvent : AudioEvent
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
        public override void PostEvent(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
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
						AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Voice, AudioAction.Play, name, soundSource.name, "Random VoiceEvent only has 1 element");
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
					var audioSwitch = AudioAssetLoader.GetAudioSwitch(AudioSwitchReference.Name);
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
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.PostEvent, name, soundSource.name);
			if (PlayLogic != VoicePlayLogic.Single)
				Clip = GetClip(soundSource);
			if (Clip == null)
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Voice, AudioAction.Play, name, soundSource.name, "Audio Clip is missing!");
				return;
			}

			var vei = CreateChildEmitter<VoiceEventInstance>(soundSource);
			vei.Init(this, soundSource);
			vei.Play(fadeInTime);					
		}		
				
		public override void Stop(GameObject soundSource, float fadeOutTime)
		{			
			if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.StopEvent, name, soundSource.name);									
			foreach (var vci in VoiceEventInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Stop(fadeOutTime);
			}				
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
			
			evt.Clip.LoadAudioData();
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
			_voiceEvent.Clip.UnloadAudioData();
			_voiceEvent.Dispose();
		}
		#endregion
		
		#region Playback				
		
		public void Play(float fadeInTime, Action<GameObject> endCallback = null)
		{
			OnAudioEnd = endCallback;
			StartCoroutine(AudioSource.Play(fadeInTime));	
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.Play, AudioSource.clip.name, gameObject.name);
		}

		private void FixedUpdate()
		{
			if (AudioSource.timeSamples < TimeSamples && !AudioSource.loop)
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Voice, AudioAction.End, AudioSource.clip.name, gameObject.name);
				AudioEnd();
			}
			TimeSamples = AudioSource.timeSamples;
		}
		
		#endregion
	}
}