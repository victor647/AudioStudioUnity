using UnityEngine;

namespace AudioStudio
{
	[AddComponentMenu("AudioStudio/AudioTag")]
	[DisallowMultipleComponent]
	public class AudioTag : AsComponent
	{
		[EnumFlag(typeof(AudioTags))]
		public AudioTags Tags;
	}
}