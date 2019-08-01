using UnityEngine;

namespace AudioStudio.Components
{
	public class AudioVisualizer : MonoBehaviour
	{
		
		public int BandsPerOctave = 1;
		public bool EqualPitchBand = true;
		private int TotalBands => BandsPerOctave * 10;
		public GameObject BandPrefab;
		private GameObject[] _bandObjects;

		private void Start()
		{
			_bandObjects = new GameObject[TotalBands];
			var distance = BandPrefab.transform.localScale.x + 0.1f;
			for (var i = 0; i < TotalBands; i++)
			{
				_bandObjects[i] = Instantiate(BandPrefab, transform);
				_bandObjects[i].transform.Translate((i - TotalBands / 2) * distance * Vector3.right);
			}
		}

		private void Update()
		{
			if (!MicrophoneInput.Instance) return;
			var ampData = EqualPitchBand ? MicrophoneInput.Instance.GetEqualPowerBands(TotalBands) : MicrophoneInput.Instance.GetFrequencyBands(TotalBands);
			for (var i = 0; i < _bandObjects.Length; i++)
			{
				var scale = _bandObjects[i].transform.localScale;
				var newHeight = 0.2f + ampData[i] * 50f;
				if (newHeight > scale.y)
					scale.y = newHeight;
				else
					scale.y -= 0.2f;
				_bandObjects[i].transform.localScale = scale;
			}
		}
	}
}