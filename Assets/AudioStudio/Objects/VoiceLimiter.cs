using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{
	public class AudioVoiceInstance
	{
		public SoundClipInstance SoundClipInstance;
		public byte Priority;

		public float Distance
		{
			get
			{
				if (SoundClipInstance.SoundClip.SpatialBlend == 0f)
					return 0;
				return ListenerManager.GetListenerDistance(SoundClipInstance.Emitter);
			}
		}
		
		public float PlayTime;
	}	
	
	public enum VoiceRemovalRule
	{
		Oldest,
		Farthest,
		LowestPriority,
		DiscardNew			
	}
	
	[CreateAssetMenu(fileName = "New Voice Limiter", menuName = "AudioStudio/Sound/Voice Limiter")]
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
					DebugMessage(soundName, toBeRemoved, "new voices won't play");
					return false;
				case VoiceRemovalRule.Oldest:					
					foreach (var v in _voices)
					{
						if (v.PlayTime < toBeRemoved.PlayTime) toBeRemoved = v;
					}
					DebugMessage(soundName, toBeRemoved, "oldest voice stops");
					break;					
				case VoiceRemovalRule.Farthest:					
					foreach (var v in _voices)
					{
						if (v.Distance > toBeRemoved.Distance) toBeRemoved = v;
					}					
					DebugMessage(soundName, toBeRemoved, "farthest voice stops");
					break;
				case VoiceRemovalRule.LowestPriority:
					foreach (var v in _voices)
					{
						if (v.Priority < toBeRemoved.Priority) toBeRemoved = v;
					}
					DebugMessage(soundName, toBeRemoved, "voice with lowest priority stops");
					break;
			}
			toBeRemoved.SoundClipInstance.Stop(FadeOutTime);
			_voices.Remove(toBeRemoved);
			
			return true;
		}

		private void DebugMessage(string soundName, AudioVoiceInstance toBeRemoved, string rule)
		{
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.SFX, AudioAction.VoiceLimit, AudioTriggerSource.Code, soundName, toBeRemoved.SoundClipInstance.Emitter, "Voice limit of " + MaxVoicesLimit + " reached, " + rule);
		}
	}
}

