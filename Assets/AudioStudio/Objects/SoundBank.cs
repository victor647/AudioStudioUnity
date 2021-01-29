using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AudioStudio.Configs
{
	[CreateAssetMenu(fileName = "New Bank", menuName = "AudioStudio/SoundBank")]
	public class SoundBank : AudioConfig
	{
		#region Fields
		public bool UseLoadCounter;
		public List<AudioEvent> AudioEvents = new List<AudioEvent>();
		public List<AudioController> AudioControllers = new List<AudioController>();
		#endregion
		
		#region Playback
		public void Load()
		{
			if (IsValid())
				BankManager.LoadBank(this);
		}

		public void Unload()
		{
			if (IsValid())
				BankManager.UnloadBank(this);
		}
		#endregion
		
		#region Editor		
		public string EventsFolder;
		
		public void RegisterEvent(AudioEvent evt)
		{
			if (AudioEvents.Contains(evt)) return;			
			AudioEvents.Add(evt);
		}
		
		public void UnregisterEvent(AudioEvent evt)
		{
			if (!AudioEvents.Contains(evt)) return;			
			AudioEvents.Remove(evt);			
		}
		
		public void RegisterController(AudioController ac)
		{
			if (AudioControllers.Contains(ac)) return;
			AudioControllers.Add(ac);			
		}

		public void UnRegisterController(AudioController ac)
		{
			if (!AudioControllers.Contains(ac)) return; 
			AudioControllers.Remove(ac);			
		}

		public void Sort()
		{
			AudioEvents.Sort();
			AudioControllers.Sort();
		}
		
		public override void CleanUp()
		{
			var tempEventList = new List<AudioEvent>(AudioEvents);                    
			foreach (var ae in tempEventList)
			{
				if (!ae) AudioEvents.Remove(ae);
			}
			var tempControllerList = new List<AudioController>(AudioControllers);                    
			foreach (var ac in tempControllerList)
			{
				if (!ac) AudioControllers.Remove(ac);
			}
		}
		
		public override bool IsValid()
		{
			return AudioEvents.Any(e => e != null) || AudioControllers.Any(c => c != null);
		}
		#endregion
	}
}