using UnityEngine;

namespace AudioStudio.Components
{
	[AddComponentMenu("AudioStudio/Audio Tag")]
	[DisallowMultipleComponent]
	public class AudioTag : AsComponent
	{
		[EnumFlag(typeof(AudioTags))]
		public AudioTags Tags;
	}
}