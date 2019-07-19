using System.Collections;
using UnityEngine;


namespace AudioStudio
{
	[AddComponentMenu("AudioStudio/PeriodSound")]
	[DisallowMultipleComponent]
	public class PeriodSound : AudioOnOffHandler
	{

		public float InitialDelay;
		public float MinInterval = 5;
		public float MaxInterval = 10;
		public AudioEventReference AudioEvent = new AudioEventReference();		
		
		private bool _isPlaying;
		private bool _isActive;
		
		protected override void HandleEnableEvent()
		{			
			_isActive = true;			
			AudioManager.DebugToProfiler(MessageType.Component, ObjectType.PeriodSound, AudioAction.Activate, "OnEnable", gameObject.name);
			StartCoroutine(PlaySound());
		}

		protected override void HandleDisableEvent()
		{
			_isActive = false;
			AudioManager.DebugToProfiler(MessageType.Component, ObjectType.PeriodSound, AudioAction.Deactivate, "OnDisable", gameObject.name);			
		}

		private IEnumerator PlaySound()
		{
			if (_isPlaying) yield break;
			_isPlaying = true;
			yield return new WaitForSeconds(InitialDelay);			
			while (_isActive)
			{
				AudioEvent.Post(gameObject);		
				yield return new WaitForSeconds(Random.Range(MinInterval, MaxInterval));
			}
			_isPlaying = false;
		}

		public override bool IsValid()
		{
			return AudioEvent.IsValid();
		}
	}
}