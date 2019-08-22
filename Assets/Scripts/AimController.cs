using AudioStudio;
using AudioStudio.Components;
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
        if (MicrophoneInput.Instance)
            MicrophoneInput.Instance.StartRecording();
        AudioAssetLoader.LoadInstrument("Glass");
    }

    private float _sizeBuffer;
	
    private void Update ()
    {
        if (!MicrophoneInput.IsRecording) return;
        var size = MicrophoneInput.Instance.Amplitude * AmplitudeScale;
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
            transform.localScale = Vector3.one;
            var noteData = MidiManager.Instance.GetHighestNoteHolding();
            if (noteData != 255)
            {
                tempPosition.y = _originalPosition.y + (noteData - LowestMidiNote) * MidiHeightScale;
            }
            else if (tempPosition.y > _originalPosition.y)
                tempPosition.y -= MicHeightSlewSpeed;
        }
                                          		
        tempPosition.x = _originalPosition.x + MidiManager.Instance.GetPitchBend() * PitchBendScale;
        transform.position = tempPosition;
    }

    private void OnDisable()
    {
        MicrophoneInput.Instance.EndRecording();
    }
}