using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Midi
{
    public class MidiManager
    {
        private static MidiManager _instance;
        public static MidiManager Instance => _instance ?? (_instance = new MidiManager());
        
        private class MidiChannelData
        {
            public readonly byte[] NoteValues = new byte[128];
            public readonly byte[] ControlChanges = new byte[128];
            public MidiMessage PitchBendData;
        }

        public float NoteToFrequency(byte note)
        {
            return Mathf.Pow(2, note / 12f) * 8.1757989f;
        }
        
        public byte FrequencyToNote(float frequency)
        {
            return (byte)(Mathf.Log(frequency / 8.1757989f, 2f) * 12f);
        }
        
        public byte GetNoteVelocity(byte noteNumber, byte channel = 0)
        {
            return _midiChannels[channel].NoteValues[noteNumber];
        }

        public bool IsNoteOn(byte noteNumber, byte channel = 0)
        {
            foreach (var evt in _noteOnEvents)
            {
                if (evt.Channel == channel || channel == 0)
                {
                    if (evt.DataByte1 == noteNumber) return true;
                }
            }
            return false;
        }

        public bool IsNoteOff(byte noteNumber, byte channel = 0)
        {
            foreach (var evt in _noteOffEvents)
            {
                if (evt.Channel == channel || channel == 0)
                {
                    if (evt.DataByte1 == noteNumber) return true;
                }
            }
            return false;
        }
        
        public bool IsNoteHolding(byte noteNumber, byte channel = 0)
        {
            return _midiChannels[channel].NoteValues[noteNumber] > 0;
        }
        
        public byte GetLowestNoteHolding(byte channel = 0)
        {
            for (byte i = 0; i < 128; i++)
            {
                var velocity = _midiChannels[channel].NoteValues[i];
                if (velocity > 0)
                    return i;
            }
            return 255;
        }
        
        public byte GetHighestNoteHolding(byte channel = 0)
        {
            for (byte i = 127; i < 255; i--)
            {
                var velocity = _midiChannels[channel].NoteValues[i];
                if (velocity > 0)
                    return i;
            }
            return 255;
        }
        
        public byte[] GetAllNotesHolding(byte channel = 0)
        {
            var keyList = new List<byte>();
            for (byte i = 0; i < 128; i++)
            {
                var velocity = _midiChannels[channel].NoteValues[i];
                if (velocity > 0)
                    keyList.Add(i);
            }
            return keyList.ToArray();
        }
        
        public MidiMessage GetNoteOn(byte channel = 0)
        {
            if (_noteOnEvents.Count == 0) return null;
            return channel == 0 ? _noteOnEvents.Peek() : _noteOnEvents.Last(m => m.Channel == channel);
        }
        
        public MidiMessage GetNoteOff(byte channel = 0)
        {
            if (_noteOffEvents.Count == 0) return null;
            return channel == 0 ? _noteOffEvents.Peek() : _noteOffEvents.Last(m => m.Channel == channel);
        }
        
        public MidiMessage[] GetAllNotesOn(byte channel = 0)
        {
            return channel == 0 ? _noteOnEvents.ToArray() : _noteOnEvents.Where(m => m.Channel == channel).ToArray();
        }
        
        public MidiMessage[] GetAllNotesOff(byte channel = 0)
        {
            return channel == 0 ? _noteOffEvents.ToArray() : _noteOffEvents.Where(m => m.Channel == channel).ToArray();
        }

        public byte GetControlValue(byte controlNumber, byte channel = 0)
        {
            return _midiChannels[channel].ControlChanges[controlNumber];
        }
        
        public int GetPitchBend(byte channel = 0)
        {
            var data = _midiChannels[channel].PitchBendData;
            return data?.PitchBendValue ?? 0;
        }

        #region Event Delegates
        public Action<MidiMessage> NoteOnCallback;
        public Action<MidiMessage> NoteOffCallback;
        public Action<MidiMessage> ControlChangeCallback;
        public Action<MidiMessage> PitchBendCallback;

        #endregion

        private readonly MidiChannelData[] _midiChannels = new MidiChannelData[17];
        private Stack<MidiMessage> _noteOnEvents = new Stack<MidiMessage>();
        private Stack<MidiMessage> _noteOffEvents = new Stack<MidiMessage>();

        private MidiManager()
        {
            for (var i = 0; i < 17; i++)
                _midiChannels[i] = new MidiChannelData();
        }

        public void Update()
        {
            _noteOnEvents.Clear();
            _noteOffEvents.Clear();
            ReceiveMidiData();
        }

        private void ReceiveMidiData()
        {
            var data = DequeueIncomingData();
            if (data == 0) return;
            
            var message = new MidiMessage(data);

            //Note on
            if (message.StatusCode == 9 && message.DataByte2 > 0)
            {
                _noteOnEvents.Push(message);
                _midiChannels[message.Channel].NoteValues[message.DataByte1] = _midiChannels[0].NoteValues[message.DataByte1] = message.DataByte2;
                NoteOnCallback?.Invoke(message);
            }

            //Note off
            if (message.StatusCode == 8 || (message.StatusCode == 9 && message.DataByte2 == 0))
            {
                _noteOffEvents.Push(message);
                _midiChannels[message.Channel].NoteValues[message.DataByte1] = _midiChannels[0].NoteValues[message.DataByte1] = 0;
                NoteOffCallback?.Invoke(message);
            }

            switch (message.StatusCode)
            {
                case 11: //Control change
                    _midiChannels[0].ControlChanges[message.DataByte1] = _midiChannels[message.Channel].ControlChanges[message.DataByte1] = message.DataByte2;
                    ControlChangeCallback?.Invoke(message);
                    break;
                case 14: //Pitch bend
                    _midiChannels[0].PitchBendData = _midiChannels[message.Channel].PitchBendData = message;
                    PitchBendCallback?.Invoke(message);
                    break;
            }
            AsUnityHelper.OutputToMidiConsole(message);
            ReceiveMidiData();
        }
        
        [DllImport("MidiJackPlugin", EntryPoint="MidiJackDequeueIncomingData")]
        public static extern ulong DequeueIncomingData();
    }
}
