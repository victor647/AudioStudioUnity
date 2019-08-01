using System.Collections.Generic;
using UnityEngine;

namespace AudioStudio.Configs
{
	public class AudioVoiceInstance
	{
		public SoundClipInstance SoundClipInstance;
		public byte Priority;
		public float Distance;
		public float PlayTime;
	}	
	
	public enum VoiceRemovalRule
	{
		Oldest,
		Farthest,
		LowestPriority,
		DiscardNew			
	}
	
	[CreateAssetMenu(fileName = "New Voice Limiter", menuName = "Audio/Sound/Voice Limiter")]
	public class VoiceLimiter : ScriptableObject
	{
		public VoiceRemovalRule VoiceRemovalRule = VoiceRemovalRule.Oldest;
		private readonly List<AudioVoiceInstance> _voices = new List<AudioVoiceInstance>();
		public byte MaxVoicesLimit = 16;		
		public float FadeOutTime = 0.1f;		

		public bool AddVoice(AudioVoiceInstance voice)
		{						
			if (_voices.Count >= MaxVoicesLimit)
			{				 
				if (!RemoveVoice()) //discard new voice will return false
					return false;	
			}			
			_voices.Add(voice);
			return true;
		}

		public void RemoveVoice(AudioVoiceInstance voice)
		{
			_voices.Remove(voice);
		}
		
		private bool RemoveVoice()
		{
			var toBeRemoved = _voices[0];
			var soundName = toBeRemoved.SoundClipInstance.Name;
			switch (VoiceRemovalRule)
			{
				case VoiceRemovalRule.DiscardNew:
					DebugMessage(soundName, "new voices won't play");
					return false;
				case VoiceRemovalRule.Oldest:					
					foreach (var v in _voices)
					{
						if (v.PlayTime < toBeRemoved.PlayTime) toBeRemoved = v;
					}
					DebugMessage(soundName, "oldest voice stops");
					break;					
				case VoiceRemovalRule.Farthest:					
					foreach (var v in _voices)
					{
						if (v.Distance > toBeRemoved.Distance) toBeRemoved = v;
					}					
					DebugMessage(soundName, "farthest voice stops");
					break;
				case VoiceRemovalRule.LowestPriority:
					foreach (var v in _voices)
					{
						if (v.Priority < toBeRemoved.Priority) toBeRemoved = v;
					}
					DebugMessage(soundName, "voice with lowest priority stops");
					break;
			}
			toBeRemoved.SoundClipInstance.Stop(FadeOutTime);
			_voices.Remove(toBeRemoved);
			
			return true;
		}

		private void DebugMessage(string soundName, string rule)
		{
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Sound, AudioAction.VoiceLimit, soundName, null, "Voice limit of " + MaxVoicesLimit + " reached, " + rule);
		}
	}
}

