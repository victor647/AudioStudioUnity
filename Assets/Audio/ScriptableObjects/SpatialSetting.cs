using UnityEngine;

namespace AudioStudio.Configs
{
	public enum RollOffMode
	{
		Linear,
		Logarithmic
	}
	
	[CreateAssetMenu(fileName = "New Spatial Setting", menuName = "Audio/Spatial Setting")]
	public class SpatialSetting : ScriptableObject
	{
		public float MinDistance = 1f;
		public float MaxDistance = 50f;
		public float SpatialBlend = 1f;
		public float Spread = 0f;
		public float DopplerLevel = 1f;		
		public RollOffMode RollOffMode = RollOffMode.Logarithmic;

		public void ApplySettings(AudioSource source)
		{			
			source.rolloffMode = RollOffMode == RollOffMode.Linear ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;				
			source.minDistance = MinDistance;
			source.maxDistance = MaxDistance;
			source.spatialBlend = SpatialBlend;
			source.spread = Spread;
			source.dopplerLevel = DopplerLevel;
		}
	}
}