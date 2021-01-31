using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	[AddComponentMenu("AudioStudio/Simple Audio Player")]
	public class SimpleAudioPlayer : AsTriggerHandler
	{
		public bool IsGlobal;
		public AudioEvent[] AudioEvents = new AudioEvent[0];

		public override void Activate(int index = 0)
		{
			PostEvents(AudioEvents, index);
		}

		public override void Deactivate(int index = 0)
		{
			StopEvents(AudioEvents, index);
		}
		
		protected override void HandleEnableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable || AudioEvents.Length == 0) return;
			Activate();
		}
        
		protected override void HandleDisableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable) return;
			Deactivate();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || AudioEvents.Length == 0 || !CompareAudioTag(other)) return;
			Activate();
		}

		private void OnTriggerExit(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;
			Deactivate();                           
		}      
        
		private void OnCollisionEnter(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || AudioEvents.Length == 0 || !CompareAudioTag(other.collider)) return;
			Activate();                           
		}

		private void OnCollisionExit(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;
			Deactivate();                           
		} 

        private void PostEvents(AudioEvent[] audioEvents, int index = 0)
        {
	        for (var i = 0; i < audioEvents.Length; i++)
	        {
		        if (index == 0 || index == i + 1)
		        {
			        var audioEvent = audioEvents[i];
			        if (audioEvent is SoundContainer @sound)
				        AudioManager.PlaySound(@sound, IsGlobal ? null : GetEmitter, 0, null,
					        AudioTriggerSource.SimpleAudioPlayer);
			        else if (audioEvent is MusicContainer @music)
				        AudioManager.PlayMusic(@music, 0, gameObject, AudioTriggerSource.SimpleAudioPlayer);
			        else if (audioEvent is VoiceEvent @voice)
				        AudioManager.PlayVoice(@voice, IsGlobal ? null : GetEmitter, 0, null,
					        AudioTriggerSource.SimpleAudioPlayer);
		        }
	        }
        }
        
        private void StopEvents(AudioEvent[] audioEvents, int index = 0)
        {
	        for (var i = 0; i < audioEvents.Length; i++)
	        {
		        if (index == 0 || index == i + 1)
		        {
			        var audioEvent = audioEvents[i];
			        if (audioEvent is SoundContainer @sound)
				        AudioManager.StopSound(@sound, IsGlobal ? null : GetEmitter, 0, AudioTriggerSource.SimpleAudioPlayer);
			        else if (audioEvent is MusicContainer @music)
				        AudioManager.StopMusic(0f, gameObject, AudioTriggerSource.SimpleAudioPlayer);
			        else if (audioEvent is VoiceEvent @voice)
				        AudioManager.StopVoice(@voice, IsGlobal ? null : GetEmitter, 0.2f,
					        AudioTriggerSource.SimpleAudioPlayer);
		        }
	        }
        }
        
        public override bool IsValid()
        {
	        return AudioEvents.Any(s => s.IsValid());
        }
	}
}