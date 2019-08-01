using System;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
	[Serializable]
	public class KeyboardMapping
	{
		public bool MultiNote;
		[Range(0, 127)]
		public byte LowestNote;
		[Range(0, 127)]
		public byte CenterNote;
		[Range(0, 127)]
		public byte HighestNote;
		public AudioClip[] Samples = new AudioClip[0];
	}

	public struct KeyMapping
	{
		public AudioClip[] NoteClips;
		public float Pitch;
	}

	public enum InstrumentSampleType
	{
		OneShotTrigger,
		SustainSingle,
		SustainLooping
	}
	
	[CreateAssetMenu(fileName = "New Music Instrument", menuName = "Audio/Music/Instrument")]
	public class MusicInstrument : ScriptableObject
	{
		public InstrumentSampleType SampleType;
		[Range(1, 8)]
		public byte MaxPolyphonicVoices = 4;
		public int Attack = 0;
		public int Release = 100;
		public AnimationCurve VelocityCurve = AnimationCurve.Linear(0, 0, 1, 1);
		public List<KeyboardMapping> KeyboardMappings = new List<KeyboardMapping>();
		private MusicInstrumentInstance _instrument;
		private KeyMapping[] _keyMappings;

		public void Init(byte channel)
		{
			_keyMappings = new KeyMapping[128];
			foreach (var mapping in KeyboardMappings)
			{
				foreach (var clip in mapping.Samples)
				{
					clip.LoadAudioData();
				}

				if (mapping.MultiNote)
				{
					for (var i = mapping.LowestNote; i < mapping.HighestNote; i++)
					{
						_keyMappings[i].NoteClips = mapping.Samples;
						_keyMappings[i].Pitch = Mathf.Pow(2, (i - mapping.CenterNote) / 12f);
					}
				}
				else
				{
					_keyMappings[mapping.CenterNote].NoteClips = mapping.Samples;
					_keyMappings[mapping.CenterNote].Pitch = 1f;
				}
			}
			_instrument = GlobalAudioEmitter.InstrumentRack.AddComponent<MusicInstrumentInstance>();
			_instrument.Init(this, channel);
			
		}

		public void Dispose()
		{
			foreach (var mapping in KeyboardMappings)
			{
				foreach (var clip in mapping.Samples)
				{
					clip.UnloadAudioData();
				}
			}
			Destroy(_instrument);
		}

		public KeyMapping GetClip(byte noteNumber)
		{
			return _keyMappings[noteNumber];
		}

		public KeyboardMapping GetMappingByCenterNote(byte centerNote)
		{
			foreach (var mapping in KeyboardMappings)
			{
				if (mapping.CenterNote == centerNote) return mapping;
			}

			var newMapping = new KeyboardMapping();
			KeyboardMappings.Add(newMapping);
			return newMapping;
		}
	}

	public class MidiVoice
	{
		public AudioSource AudioSource;
		public byte Note;
	}
	
	public class MusicInstrumentInstance : MonoBehaviour
	{
		private byte _channel;
		private MusicInstrument _instrument;
		private MidiVoice[] _voices;
		private AudioSource _source;

		public void Init(MusicInstrument instrument, byte channel)
		{
			_instrument = instrument;
			_channel = channel;
			if (instrument.SampleType != InstrumentSampleType.OneShotTrigger)
			{
				_voices = new MidiVoice[instrument.MaxPolyphonicVoices];
				for (var i = 0; i < _voices.Length; i++)
				{
					_voices[i] = new MidiVoice {AudioSource = gameObject.AddComponent<AudioSource>()};
				}
			}
			else
				_source = gameObject.AddComponent<AudioSource>();
		}

		private void OnDestroy()
		{
			if (_instrument.SampleType != InstrumentSampleType.OneShotTrigger)
			{
				foreach (var voice in _voices)
				{
					Destroy(voice.AudioSource);
				}
			}
			else
				Destroy(_source);
		}

		private void Update()
		{
			switch (_instrument.SampleType)
			{
				case InstrumentSampleType.OneShotTrigger:
					PlayOneShot();
					break;
				case InstrumentSampleType.SustainSingle:
					PlaySustain(false);
					break;
				case InstrumentSampleType.SustainLooping:
					PlaySustain(true);
					break;
			}
		}

		private void PlayOneShot()
		{
			var notesOn = MidiManager.Instance.GetAllNotesOn(_channel);
			if (notesOn.Length == 0) return;
			foreach (var message in notesOn)
			{
				var note = message.DataByte1;
				var velocity = message.DataByte2;
				var keyMapping = _instrument.GetClip(note);
				var clips = keyMapping.NoteClips;
				var clip = clips[Random.Range(0, clips.Length)];
				_source.pitch = keyMapping.Pitch;
				var volume = _instrument.VelocityCurve.Evaluate(velocity / 127f);
				_source.PlayOneShot(clip, volume);
			}
		}
		
		private void PlaySustain(bool loop)
		{
			var notesOff = MidiManager.Instance.GetAllNotesOff(_channel);
			foreach (var message in notesOff)
			{
				foreach (var voice in _voices)
				{
					if (voice.Note == message.DataByte1)
						StartCoroutine(voice.AudioSource.Stop(_instrument.Release / 1000f));
				}
			}
			var notesOn = MidiManager.Instance.GetAllNotesOn(_channel);
			foreach (var message in notesOn)
			{
				var voice = GetFirstIdleVoice();
				if (voice == null) break;
				var note = message.DataByte1;
				var velocity = message.DataByte2;
				var keyMapping = _instrument.GetClip(note);
				var clips = keyMapping.NoteClips;
				voice.Note = note;
				voice.AudioSource.clip = clips[Random.Range(0, clips.Length)];
				voice.AudioSource.pitch = keyMapping.Pitch;
				voice.AudioSource.volume = _instrument.VelocityCurve.Evaluate(velocity / 127f);
				voice.AudioSource.loop = loop;
				StartCoroutine(voice.AudioSource.Play(_instrument.Attack / 1000f));
			}
		}

		private MidiVoice GetFirstIdleVoice()
		{
			return _voices.FirstOrDefault(voice => !voice.AudioSource.isPlaying);
		}
	}
}