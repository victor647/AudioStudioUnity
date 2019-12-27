using System;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Serialization;

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
	
	public abstract class AudioObjectReference
	{
		public string Name;		
		
		public override bool Equals(object obj)
		{
			var other = obj as AudioObjectReference;
			if (other != null) 
				return Name == other.Name;
			return false;
		}

		protected bool Equals(AudioObjectReference other)
		{
			return string.Equals(Name, other.Name);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
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
	public class PostEventReference : AudioObjectReference
	{        
		[FormerlySerializedAs("EventType")]
		public AudioEventType Type = AudioEventType.SFX;
		public AudioEventAction Action = AudioEventAction.Play;	
		public float FadeTime;
		public PostEventReference(string name)
		{			
			Name = name;
		}
		
		public PostEventReference()
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

		public void Play(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
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
        
		public void Stop(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
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
		
		public void Mute(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.MuteMusic(FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.MuteSound(Name, soundSource, FadeTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.MuteVoice(Name, soundSource, FadeTime, trigger);
					break;
			}
		}
		
		public void UnMute(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.UnMuteMusic(FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.UnMuteSound(Name, soundSource, FadeTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.UnMuteVoice(Name, soundSource, FadeTime, trigger);
					break;
			}
		}
		
		public void Pause(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.PauseMusic(FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.PauseSound(Name, soundSource, FadeTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.PauseVoice(Name, soundSource, FadeTime, trigger);
					break;
			}
		}
		
		public void Resume(GameObject soundSource = null, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (Type)
			{
				case AudioEventType.Music:
					AudioManager.ResumeMusic(FadeTime, soundSource, trigger);
					break;
				case AudioEventType.SFX:
					AudioManager.ResumeSound(Name, soundSource, FadeTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.ResumeVoice(Name, soundSource, FadeTime, trigger);
					break;
			}
		}
        
		public override bool Equals(object obj)
		{
			var other = obj as PostEventReference;
			if (other != null) 
				return base.Equals(obj) && Type == other.Type;
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
		public AudioParameterReference(string name)
		{			
			Name = name;
		}	
		
		public AudioParameterReference()
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
		public SoundBankReference(string name)
		{			
			Name = name;
		}
		
		public SoundBankReference()
		{			
		}
		
		public void Load(AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.LoadBank(Name, trigger);		
		}
		
		public void Unload(AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;			
			AudioManager.UnloadBank(Name, trigger);		
		}
	}
	
	[Serializable]
	public class AudioSwitchReference : AudioObjectReference
	{
		public AudioSwitchReference(string groupName)
		{			
			Name = groupName;
		}
		
		public AudioSwitchReference()
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
		
		public SetSwitchReference()
		{			
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