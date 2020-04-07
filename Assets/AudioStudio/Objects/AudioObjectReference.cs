using System;
using System.Linq;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{
	public enum AudioEventType
	{
		SFX,
		Music,
		Voice,
	}
	
	public enum AudioEventAction
	{
		Play,
		Stop,
		Mute,
		Unmute,
		Pause,
		Resume
	}
	
	public enum SoundBankAction
	{
		Load,
		Unload
	}
	
	public abstract class AudioObjectReference
	{
		public string Name;

		protected AudioObjectReference(string name = "")
		{			
			Name = name;
		}

		public override bool Equals(object obj)
		{
			if (obj is AudioObjectReference other)
				return Name == other.Name;
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(Name);
		}
	}

	[Serializable]
	public class MusicTransitionReference : AudioObjectReference
	{
	}
	
	[Serializable]
	public class MusicSegmentReference : AudioObjectReference
	{
	}
	
	[Serializable]
	public class AudioEventReference : AudioObjectReference
	{
		public AudioEventType Type = AudioEventType.SFX;
		
		public AudioEventReference(AudioEvent audioEvent)
		{
			Name = audioEvent.name;
			switch (audioEvent)
			{
				case SoundContainer _:
					Type = AudioEventType.SFX;
					break;
				case MusicContainer _:
					Type = AudioEventType.Music;
					break;
				case VoiceEvent _:
					Type = AudioEventType.Voice;
					break;
			}
		}
		
		public AudioEventReference(string name = "") : base(name)
		{
		}

		internal void Play(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.PlayMusic(Name, 0, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.PlaySound(Name, soundSource, 0, null, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.PlayVoice(Name, soundSource, 0, null, trigger);
					break;
			}
		}
		
		public override bool Equals(object obj)
		{
			if (obj is AudioEventReference other)
				return base.Equals(other) && Type == other.Type;
			return false;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	[Serializable]
	public class PostEventReference : AudioEventReference
	{
		public AudioEventAction Action = AudioEventAction.Play;	
		public float FadeTime;
		
		public PostEventReference(string name = "") : base(name)
		{
		}
		
		public PostEventReference(AudioEvent audioEvent) : base(audioEvent)
		{
		}
		
		public void Post(GameObject go = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Action)
			{
				case AudioEventAction.Play:
					Play(go, trigger);
					break;
				case AudioEventAction.Stop:
					Stop(go, trigger);
					break;
				case AudioEventAction.Mute:
					Mute(go, trigger);
					break;
				case AudioEventAction.Unmute:
					UnMute(go, trigger);
					break;
				case AudioEventAction.Pause:
					Pause(go, trigger);
					break;
				case AudioEventAction.Resume:
					Resume(go, trigger);
					break;
			}
		}
		
		public void Cancel(GameObject go = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Action)
			{
				case AudioEventAction.Play:
					Stop(go, trigger);
					break;
				case AudioEventAction.Stop:
					Play(go, trigger);
					break;
				case AudioEventAction.Mute:
					UnMute(go, trigger);
					break;
				case AudioEventAction.Unmute:
					Mute(go, trigger);
					break;
				case AudioEventAction.Pause:
					Resume(go, trigger);
					break;
				case AudioEventAction.Resume:
					Pause(go, trigger);
					break;
			}
		}

		private new void Play(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.PlayMusic(Name, FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.PlaySound(Name, soundSource, FadeTime, null, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.PlayVoice(Name, soundSource, FadeTime, null, trigger);
					break;
			}
		}

		private void Stop(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.StopMusic(FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.StopSound(Name, soundSource, FadeTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.StopVoice(Name, soundSource, FadeTime, trigger);
					break;
			}
		}

		private void Mute(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			AudioManager.MuteEvent(Name, soundSource, FadeTime, trigger);
		}

		private void UnMute(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			AudioManager.UnMuteEvent(Name, soundSource, FadeTime, trigger);
		}

		private void Pause(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			AudioManager.PauseEvent(Name, soundSource, FadeTime, trigger);
		}

		private void Resume(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			AudioManager.ResumeEvent(Name, soundSource, FadeTime, trigger);
		}
        
		public override bool Equals(object obj)
		{
			if (obj is PostEventReference other)
				return base.Equals(other) && Action == other.Action && FadeTime == other.FadeTime;
			return false;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	[Serializable]
	public class AudioParameterReference : AudioObjectReference
	{			
		public AudioParameterReference(string name = "") : base(name)
		{
		}

		public void SetValue(float value, GameObject go = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.SetParameterValue(Name, value, go, trigger);			
		}

		public void SetValueGlobal(float value)
		{
			if (!IsValid()) return;
			AudioManager.SetParameterValueGlobal(Name, value);
		}
		
		public float GetValue(GameObject go = null)
		{
			return !IsValid() ? 0f : AudioManager.GetParameterValue(Name, go);
		}
	}  
	
	[Serializable]
	public class SetAudioParameterReference : AudioParameterReference
	{
		public float Value;

		public void SetValue(AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;	
			AudioManager.SetParameterValue(Name, Value, null, trigger);
		}
		
		public float GetValue()
		{			
			return !IsValid() ? 0f : AudioManager.GetParameterValue(Name);
		}
	}  
	
	[Serializable]
	public class SoundBankReference : AudioObjectReference
	{
		public SoundBankReference(string name = "") : base(name)
		{
		}

		public void Load(Action onLoadFinished = null, GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.LoadBank(Name, onLoadFinished, source, trigger);
		}
		
		public void Unload(GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.UnloadBank(Name, source, trigger);		
		}
	}
	
	[Serializable]
	public class LoadBankReference : SoundBankReference
	{
		public LoadBankReference(string name = "")
		{
			Name = name;
			UnloadOnDisable = true;
			LoadFinishEvents = new AudioEventReference[0];
		}
		
		public bool UnloadOnDisable;
		public AudioEventReference[] LoadFinishEvents;
		
		public void Load(GameObject source = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.LoadBank(Name, () =>
			{
				if (LoadFinishEvents == null) return;
				foreach (var evt in LoadFinishEvents)
				{
					evt.Play(source, trigger);	
				}
			}, source, trigger);
		}

		public override bool Equals(object obj)
		{
			if (obj is LoadBankReference other)
				return base.Equals(other) && UnloadOnDisable == other.UnloadOnDisable && LoadFinishEvents.SequenceEqual(other.LoadFinishEvents);
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
	
	[Serializable]
	public class AudioSwitchReference : AudioObjectReference
	{
		public AudioSwitchReference(string groupName = "") : base(groupName)
		{
		}

		public void SetValue(string switchName, GameObject go = null)
		{
			if (!IsValid()) return;			
			AudioManager.SetSwitch(Name, switchName, go);			
		}

		public void SetValueGlobal(string switchName)
		{
			if (!IsValid()) return;
			AudioManager.SetSwitchGlobal(Name, switchName);
		}
		
		public string GetValue(GameObject go = null)
		{
			return !IsValid() ? "" : AudioManager.GetSwitch(Name, go);
		}
	}
	
	[Serializable]
	public class SetSwitchReference : AudioSwitchReference
	{
		public string Selection;
		public string FullName => Name + " / " + Selection;
		
		public SetSwitchReference(string groupName, string switchName = "")
		{			
			Name = groupName;
			Selection = switchName;
		}

		public void SetValue(GameObject go = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.SetSwitch(Name, Selection, go, trigger);			
		}

		public void SetValueGlobal(AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			AudioManager.SetSwitchGlobal(Name, Selection, trigger);
		}
	}
}