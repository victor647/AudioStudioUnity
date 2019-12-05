using System.Collections.Generic;
using UnityEditor;
using System.Runtime.InteropServices;
using UnityEngine;
using AudioStudio.Tools;

namespace AudioStudio.Midi
{
    
    public class MidiConsole : EditorWindow
    {
        public static MidiConsole Instance;
        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }
        
        private Queue<MidiMessage> _midiMessages = new Queue<MidiMessage>();
        private Dictionary<uint, string> _deviceNames = new Dictionary<uint, string>();
        private static readonly Dictionary<int, string> _messageTypes = new Dictionary<int, string>
        {
            {8, "Note Off"},
            {9, "Note On"},
            {10, "Poly Pressure"},
            {11, "Control Change"},
            {12, "Program Change"},
            {13, "Channel Pressure"},
            {14, "Pitch Bend"},
            {15, "System Exclusive"},
        };
        private static readonly Dictionary<int, string> _noteNames = new Dictionary<int, string>
        {
            {0, "C"},
            {1, "C#/Db"},
            {2, "D"},
            {3, "D#/Eb"},
            {4, "E"},
            {5, "F"},
            {6, "F#/Gb"},
            {7, "G"},
            {8, "G#/Ab"},
            {9, "A"},
            {10, "A#/Bb"},
            {11, "B"},
        };
        
        private bool _paused;
        private bool _autoScroll = true;
        private bool _includeNoteOn = true;
        private bool _includeNoteOff = true;
        private bool _includeCC = true;
        private bool _includePitchBend = true;
        private bool _includeOther = true;   
        private Vector2 _scrollPosition;
        
        private void OnGUI()
        {
            EditorGUILayout.Separator();
            var deviceCount = CountEndpoints();
            
            EditorGUILayout.LabelField("MIDI devices", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                
                for (var i = 0; i < deviceCount; i++)
                {
                    var id = GetEndpointIdAtIndex(i);
                    var deviceName = GetEndpointName(id);
                    _deviceNames[id] = deviceName;
                    EditorGUILayout.LabelField(deviceName);
                }
            }

            DrawDisplayFilters();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Channel", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Data Byte 1", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Data Byte 2", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Device", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                _scrollPosition =  EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height - 200));
                if (_autoScroll) _scrollPosition.y = Mathf.Infinity;
                foreach (var message in _midiMessages)
                {
                    var statusCode = message.StatusByte >> 4;

                    switch (statusCode)
                    {
                        case 8:
                            if (!_includeNoteOff) continue; break;
                        case 9:
                            if (!_includeNoteOn) continue; break;
                        case 11:
                            if (!_includeCC) continue; break;
                        case 14:
                            if (!_includePitchBend) continue; break;
                        default:
                            if (!_includeOther) continue; break;
                    }
                    
                    var channelNumber = (message.StatusByte & 0xf) + 1;
                    var dataByte1 = message.DataByte1;
                    var dataByte2 = message.DataByte2;
                    var device = message.Device;

                    var db1 = dataByte1.ToString();
                    switch (statusCode)
                    {
                        case 8:
                        case 9:
                            db1 = GetNoteName(dataByte1);
                            break;
                        case 11:
                            if (dataByte1 == 1) db1 = "Modulation";
                            if (dataByte1 == 2) db1 = "Breath";
                            if (dataByte1 == 7) db1 = "Volume";
                            if (dataByte1 == 7) db1 = "Pan";
                            if (dataByte1 == 11) db1 = "Expression";
                            if (dataByte1 == 64) db1 = "Sustain Pedal";
                            break;
                        case 14:
                            db1 = "Fine " + db1;
                            break;
                    }

                    var db2 = dataByte2.ToString();
                    switch (statusCode)
                    {
                        case 9:
                            db2 = "Velocity " + db2;
                            break;
                        case 14:
                            db2 = "Coarse " + db2;
                            break;
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(_messageTypes[statusCode], GUILayout.Width(100));
                    EditorGUILayout.LabelField(channelNumber.ToString(), GUILayout.Width(60));
                    EditorGUILayout.LabelField(db1, GUILayout.Width(100));
                    EditorGUILayout.LabelField(db2, GUILayout.Width(100));
                    EditorGUILayout.LabelField(_deviceNames[device]);
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }

        private void DrawDisplayFilters()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MIDI messages", EditorStyles.boldLabel);
            if (GUILayout.Button("Select All", GUILayout.Width(100))) SelectAll(true);
            if (GUILayout.Button("Deselect All", GUILayout.Width(100))) SelectAll(false);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            _includeNoteOn = GUILayout.Toggle(_includeNoteOn, "Note On", GUILayout.Width(80));
            _includeNoteOff = GUILayout.Toggle(_includeNoteOff, "Note Off", GUILayout.Width(80));            
            _includeCC = GUILayout.Toggle(_includeCC, "CC", GUILayout.Width(60));          
            _includePitchBend = GUILayout.Toggle(_includePitchBend, "Pitch Bend", GUILayout.Width(100));
            _includeOther = GUILayout.Toggle(_includeOther, "Other", GUILayout.Width(80));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause")) _paused = true;
            if (GUILayout.Button("Resume")) _paused = false;
            if (GUILayout.Button("Clear")) _midiMessages.Clear();
            _autoScroll =  GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100));
            GUILayout.EndHorizontal();      
        }
        
        private void SelectAll(bool enabled)
        {
            _includeNoteOn = _includeNoteOff = _includeCC = _includePitchBend = _includeOther = enabled;
        }

        public void AddMessage(MidiMessage message)
        {
            _midiMessages.Enqueue(message);
            if (_midiMessages.Count > 100)
                _midiMessages.Dequeue();
            Repaint();
        }
        
        private void Update()
        {
            if (_paused || Application.isPlaying) return;
            MidiManager.Instance.Update();
        }

        public static string GetNoteName(byte noteNumber)
        {
            var octave = noteNumber / 12;
            var note = noteNumber % 12;
            return _noteNames[note] + octave;
        }

        [DllImport("MidiJackPlugin", EntryPoint="MidiJackCountEndpoints")]
        private static extern int CountEndpoints();

        [DllImport("MidiJackPlugin", EntryPoint="MidiJackGetEndpointIDAtIndex")]
        private static extern uint GetEndpointIdAtIndex(int index);

        [DllImport("MidiJackPlugin")]
        private static extern System.IntPtr MidiJackGetEndpointName(uint id);

        private static string GetEndpointName(uint id) {
            return Marshal.PtrToStringAnsi(MidiJackGetEndpointName(id));
        }
    }
}