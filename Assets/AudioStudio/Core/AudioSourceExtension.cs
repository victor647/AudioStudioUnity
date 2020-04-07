using System;
using System.Collections;
using UnityEngine;

namespace AudioStudio
{
	public static class AudioSourceExtension
	{
		public static IEnumerator Play(this AudioSource source, float fadeInTime)
		{
			var targetVolume = source.volume;
			var timeStamp = Time.time;
			source.volume = 0f;
			source.Play();
			while (Time.time - timeStamp < fadeInTime)
			{
				source.volume = Mathf.Lerp(0, targetVolume, (Time.time - timeStamp) / fadeInTime);
				yield return new WaitForFixedUpdate();
			}
		}

		public static IEnumerator Stop(this AudioSource source, float fadeOutTime, Action onAudioEnd = null)
		{
			var initialVolume = source.volume;
			var timeStamp = Time.time;
			while (Time.time - timeStamp < fadeOutTime)
			{
				source.volume = Mathf.Lerp(initialVolume, 0, (Time.time - timeStamp) / fadeOutTime);
				yield return new WaitForFixedUpdate();
			}
			source.Stop();
			onAudioEnd?.Invoke();
		}
		
		public static IEnumerator Mute(this AudioSource source, float fadeOutTime)
		{
			var initialVolume = source.volume;
			var timeStamp = Time.time;
			while (Time.time - timeStamp < fadeOutTime)
			{
				source.volume = Mathf.Lerp(initialVolume, 0, (Time.time - timeStamp) / fadeOutTime);
				yield return new WaitForFixedUpdate();
			}
			source.mute = true;
		}
		
		public static IEnumerator UnMute(this AudioSource source, float fadeInTime)
		{
			var targetVolume = source.volume;
			var timeStamp = Time.time;
			source.volume = 0f;
			source.mute = false;
			while (Time.time - timeStamp < fadeInTime)
			{
				source.volume = Mathf.Lerp(0, targetVolume, (Time.time - timeStamp) / fadeInTime);
				yield return new WaitForFixedUpdate();
			}
		}
		
		public static IEnumerator Pause(this AudioSource source, float fadeOutTime)
		{
			var initialVolume = source.volume;
			var timeStamp = Time.time;
			while (Time.time - timeStamp < fadeOutTime)
			{
				source.volume = Mathf.Lerp(initialVolume, 0, (Time.time - timeStamp) / fadeOutTime);
				yield return new WaitForFixedUpdate();
			}
			source.Pause();
		}
		
		public static IEnumerator Resume(this AudioSource source, float fadeInTime)
		{
			var targetVolume = source.volume;
			var timeStamp = Time.time;
			source.volume = 0f;
			source.UnPause();
			while (Time.time - timeStamp < fadeInTime)
			{
				source.volume = Mathf.Lerp(0, targetVolume, (Time.time - timeStamp) / fadeInTime);
				yield return new WaitForFixedUpdate();
			}
		}
}
}