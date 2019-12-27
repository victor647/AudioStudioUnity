using System;
using UnityEngine;

namespace AudioStudio.Configs
{
	[Serializable]
	public class KeyAssignment
	{
		public MusicKey Keys = MusicKey.All;
		public AudioClip Clip;
	}
	
	[CreateAssetMenu(fileName = "New Music Stinger", menuName = "AudioStudio/Music/Stinger")]
	public class MusicStinger : MusicContainer
	{
		public float PickUpLength;
		public KeyAssignment[] KeyAssignments = new KeyAssignment[1];
		public TransitionInterval TriggerSync = TransitionInterval.Immediate;

		public override void Play(GameObject soundSource, float fadeInTime = 0f, Action<GameObject> endCallback = null)
		{
			MusicTransport.Instance.QueueStinger(this);
		}
	}
}

