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
	
	[CreateAssetMenu(fileName = "New Voice Event", menuName = "AudioStudio/Voice Event")]
	public class VoiceEvent : AudioEvent
	{						
		#region Fields
		public VoicePlayLogic PlayLogic = VoicePlayLogic.Single;
		// random
		public bool AvoidRepeat = true;
		private byte _lastPlayedIndex = 255;
		// switch
		public AudioSwitchReference AudioSwitchReference = new AudioSwitchReference();	
		public SwitchClipMapping[] SwitchClipMappings;
		public AudioClip Clip;		
		public List<AudioClip> Clips = new List<AudioClip>();
		private List<VoiceEventInstance> _playingInstances = new List<VoiceEventInstance>(); 
		#endregion
		
		#region Initialize					

		internal override void Init()
		{		
			_lastPlayedIndex = 255;
			_playingInstances.Clear();
			if (PlayLogic == VoicePlayLogic.Single)
			{
				if (Clip)
					Clip.LoadAudioData();
				else
					Debug.LogError("AudioClip of VoiceEvent " + name + " is missing!");
			}
			else
			{
				foreach (var clip in Clips)
				{
					if (clip)
						clip.LoadAudioData();
					else
						Debug.LogError("AudioClip of VoiceEvent " + name + " is missing!");
				}
			}
		}

		internal override void Dispose()
		{
			_playingInstances.Clear();
			switch (PlayLogic)
			{
				case VoicePlayLogic.Single:
					Clip.UnloadAudioData();
					break;
				case VoicePlayLogic.Switch:
					foreach (var mapping in SwitchClipMappings)
					{
						if (mapping.Clip)
							mapping.Clip.UnloadAudioData();
					}
					break;
				default:
					foreach (var clip in Clips)
					{
						if (clip)
							clip.UnloadAudioData();
					}
					break;
			}
		}
		
		internal void AddInstance(VoiceEventInstance instance)
		{
			_playingInstances.Add(instance);
			EmitterManager.AddVoiceInstance(instance);  
		}

		internal void RemoveInstance(VoiceEventInstance instance)
		{
			_playingInstances.Remove(instance);
			EmitterManager.RemoveVoiceInstance(instance);  
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
					while (selectedIndex == _lastPlayedIndex)
					{
						selectedIndex = Random.Range(0, Clips.Count);
					}
					_lastPlayedIndex = (byte)selectedIndex;
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
					_lastPlayedIndex++;
					if (_lastPlayedIndex == Clips.Count) _lastPlayedIndex = 0;
					return Clips[_lastPlayedIndex];		
			}
			return null;
		}
        
		public override string Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
		{
			if (!soundSource)
				return string.Empty;
			if (PlayLogic != VoicePlayLogic.Single)
				Clip = GetClip(soundSource);
			if (!Clip) return string.Empty;
			var vei = soundSource.AddComponent<VoiceEventInstance>();
			vei.Init(this, soundSource);
			vei.Play(fadeInTime);
			return Clip.name;
		}

		public override void Stop(GameObject soundSource, float fadeOutTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Stop(fadeOutTime);
			}				
		}

		internal override void StopAll(float fadeOutTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{                                
				vci.Stop(fadeOutTime);
			}
		}

		internal override void Mute(GameObject soundSource, float fadeOutTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Mute(fadeOutTime);
			}
		}

		internal override void UnMute(GameObject soundSource, float fadeInTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{
				if (vci.Emitter == soundSource)
					vci.UnMute(fadeInTime);
			}
		}

		internal override void Pause(GameObject soundSource, float fadeOutTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Pause(fadeOutTime);
			}
		}

		internal override void Resume(GameObject soundSource, float fadeInTime = 0f)
		{
			foreach (var vci in _playingInstances)
			{
				if (vci.Emitter == soundSource)
					vci.Resume(fadeInTime);
			}
		}
		#endregion

		#region Editor		
		public override void CleanUp()
		{
			switch (PlayLogic)
			{
				case VoicePlayLogic.Single:
					Clips.Clear();
					SwitchClipMappings = new SwitchClipMapping[0];
					if (!Clip)
						Debug.LogError("AudioClip of VoiceEvent " + name + " is missing!");
					break;
				case VoicePlayLogic.Random:
				case VoicePlayLogic.SequenceStep:
					Clip = null;
					SwitchClipMappings = new SwitchClipMapping[0];
					if (Clips.Any(c => !c))
						Debug.LogError("AudioClips of VoiceEvent " + name + " is missing!");
					break;
				case VoicePlayLogic.Switch:
					Clip = null;
					if (SwitchClipMappings.Any( c=> !c.Clip))
						Debug.LogError("AudioClips of VoiceEvent " + name + " is missing!");
					break;
			}
		}
		
		public override void OnValidate()
		{
			if (PlayLogic == VoicePlayLogic.Switch)
				Clips = SwitchClipMappings.Select(mapping => mapping.Clip).ToList();
		}
		
		public override bool IsValid()
		{
			return Clip != null || Clips.Any(c => c != null);
		}
		
		internal override AudioObjectType GetEventType()
		{
			return AudioObjectType.Voice;
		}
		#endregion
	}

	public class VoiceEventInstance : AudioEventInstance
	{	
		#region Initialize
		internal VoiceEvent VoiceEvent;
		
		public void Init(VoiceEvent evt, GameObject emitter)
		{			
			AudioSource = gameObject.AddComponent<AudioSource>();
			VoiceEvent = evt;
			Emitter = emitter;
			
			VoiceEvent.AddInstance(this);
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

		private void OnDisable()
		{
			OnAudioEndOrStop();
		}

		private void OnDestroy()
		{
			OnAudioEnd?.Invoke(Emitter);
			Destroy(AudioSource);
			VoiceEvent.RemoveInstance(this);
		}
		#endregion
		
		#region Playback				
		public void Play(float fadeInTime, Action<GameObject> endCallback = null)
		{
			OnAudioEnd = endCallback;
			if (isActiveAndEnabled && fadeInTime > 0f)
				StartCoroutine(AudioSource.Play(fadeInTime));
			else
				AudioSource.Play();
		}

		internal override void UpdatePlayingStatus()
		{
			if (AudioSource.timeSamples < TimeSamples && !AudioSource.loop)
			{
				AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Voice, AudioAction.End, AudioTriggerSource.Code, AudioSource.clip.name, gameObject);
				OnAudioEndOrStop();
			}
			TimeSamples = AudioSource.timeSamples;
		}
		#endregion
	}
}