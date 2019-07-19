using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioStudio
{
	public class MusicInstrument : AudioEvent
	{
		public SwitchClipMapping[] NoteMappings;
		
		public override void CleanUp()
		{
			throw new NotImplementedException();
		}

		public override void Init()
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
			throw new NotImplementedException();
		}

		public override void PostEvent(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
		{
			throw new NotImplementedException();
		}

		public override void Play(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
		{
			throw new NotImplementedException();
		}

		public override void Stop(GameObject soundSource, float fadeOutTime)
		{
			throw new NotImplementedException();
		}
	}
}