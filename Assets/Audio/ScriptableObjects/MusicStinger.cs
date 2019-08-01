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
	
	[CreateAssetMenu(fileName = "New Music Stinger", menuName = "Audio/Music/Stinger")]
	public class MusicStinger : MusicContainer
	{
		public float PickUpLength;
		public KeyAssignment[] KeyAssignments = new KeyAssignment[1];
	}
}

