using System;
using AudioStudio;
using UnityEngine;

public class AimController : MonoBehaviour
{
	public float MicHeightScale = 1f;
	public float MicHeightSlewSpeed = 0.1f;
	public float AmplitudeScale = 10f;
	public float SizeSlewSpeed = 0.05f;
	public byte LowestMidiNote = 48;
	public float MidiHeightScale = 1f;
	public float PitchBendScale = 0.001f;

	private Vector3 _originalPosition;

	private void Awake()
	{
		_originalPosition = transform.position;
	}

	private void Start () 
	{
		MicrophoneInput.Instance.StartVoiceRecord();
	}

	private float _sizeBuffer;
	
	private void Update ()
	{
		var size = MicrophoneInput.Instance.GetAmplitude() * AmplitudeScale;
		var tempPosition = transform.position;
		if (size > 0.1f)
		{
			if (size > _sizeBuffer)
				_sizeBuffer = size;
			else
				_sizeBuffer -= SizeSlewSpeed;
			transform.localScale = Vector3.one * (1 + _sizeBuffer);
			var maxBand = MicrophoneInput.Instance.GetHighestFrequencyBand();
			tempPosition.y = _originalPosition.y + maxBand * MicHeightScale;
		}
		else
		{
			MidiManager.Instance.Update();
			transform.localScale = Vector3.one;
			var noteData = MidiManager.Instance.GetNoteHolding();
			if (noteData != 255)
				tempPosition.y = _originalPosition.y + (noteData - LowestMidiNote) * MidiHeightScale;
			else if (tempPosition.y > _originalPosition.y)
				tempPosition.y -= MicHeightSlewSpeed;
		}
		
		tempPosition.x = _originalPosition.x + MidiManager.Instance.GetPitchBend() * PitchBendScale;
		transform.position = tempPosition;
	}

	private void OnDisable()
	{
		MicrophoneInput.Instance.EndVoiceRecord();
	}
}
