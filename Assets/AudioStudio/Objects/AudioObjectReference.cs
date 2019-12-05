using System;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{
	public enum AudioEventType
	{
		Sound,
		Music,
		Voice,
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
	public class AudioEventReference : AudioObjectReference
	{        
		public AudioEventType EventType = AudioEventType.Sound;	
		public AudioEventReference(string name)
		{			
			Name = name;
		}
		
		public AudioEventReference()
		{			
		}
		
		public void Post(GameObject go = null, float fadeInTime = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (EventType)
			{
				case AudioEventType.Music:
					AudioManager.PlayMusic(Name, fadeInTime, -1f, -1f, -1f, trigger);
					break;
				case AudioEventType.Sound:
					AudioManager.PlaySound(Name, go, fadeInTime, null, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.PlayVoice(Name, go, fadeInTime, null, trigger);
					break;
			}
		}       
        
		public void Stop(GameObject go = null, float fadeOutTime = -1f, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!IsValid()) return;
			switch (EventType)
			{
				case AudioEventType.Music:
					AudioManager.StopMusic(fadeOutTime);
					break;
				case AudioEventType.Sound:
					AudioManager.StopSound(Name, go, fadeOutTime, trigger);
					break;
				case AudioEventType.Voice:
					AudioManager.StopVoice(Name, go, fadeOutTime, trigger);
					break;
			}
		}
        
		public override bool Equals(object obj)
		{
			var other = obj as AudioEventReference;
			if (other != null) 
				return base.Equals(obj) && EventType == other.EventType;
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