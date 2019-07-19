#if !UNITY_EDITOR && UNITY_WEBGL	
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AudioStudio
{
	public partial class VoiceEvent
	{	
		private WebGLStreamingAudioSourceInterop _interop;

		private int GetClipName()
		{
			switch (PlayLogic)
			{					
				case VoicePlayLogic.Random:					
					var selectedIndex = Random.Range(0, ClipCount);
					if (!AvoidRepeat) return selectedIndex;
					while (selectedIndex == LastSelectedIndex)
					{
						selectedIndex = Random.Range(0, ClipCount);
					}
					LastSelectedIndex = (byte)selectedIndex;
					return selectedIndex;					
				case VoicePlayLogic.SequenceStep:
					LastSelectedIndex++;
					if (LastSelectedIndex == ClipCount) LastSelectedIndex = 0;
					return LastSelectedIndex;		
			}
			return 0;
		}

		public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
		{
			var clipName = "";
			if (PlayLogic != VoicePlayLogic.Single)
				clipName = "Vo_" + name + "_" + (GetClipName() + 1).ToString("00");
			else
				clipName = "Vo_" + name;
			_interop = new WebGLStreamingAudioSourceInterop(AudioAssetLoader.GetClipUrl(clipName, ObjectType.Voice), soundSource);
			_interop.Play();
		}

		public override void Stop(GameObject soundSource, float fadeOutTime)
		{
			if (fadeOutTime < 0f) fadeOutTime = DefaultFadeOutTime;
			_interop.Destroy();
		}		
	}
}
#endif		