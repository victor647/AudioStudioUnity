using System.Collections.Generic;
using UnityEngine;

namespace AudioStudio
{
	[CreateAssetMenu(fileName = "New Bank", menuName = "Audio/SoundBank")]
	public class SoundBank : AudioObject
	{
		#region Initialize
		public static int GlobalBankCount;

		public List<SoundContainer> AudioEvents = new List<SoundContainer>();
		public List<AudioController> AudioControllers = new List<AudioController>();

		public void Init()
		{
			GlobalBankCount++;
		}

		public void Dispose()
		{
			GlobalBankCount--;
		}		
		#endregion
		
		#region Editor		
		public void RegisterEvent(SoundContainer evt)
		{
			if (AudioEvents.Contains(evt)) return;			
			AudioEvents.Add(evt);
		}
		
		public void UnregisterEvent(SoundContainer evt)
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
		
		public override void CleanUp()
		{
			var tempEventList = new List<SoundContainer>(AudioEvents);                    
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
		#endregion
		
		#region Playback		
		public void Load()
		{
			AudioManager.LoadBank(name);
		}
		
		public void Unload()
		{
			AudioManager.UnloadBank(name);
		}
		#endregion
	}
}