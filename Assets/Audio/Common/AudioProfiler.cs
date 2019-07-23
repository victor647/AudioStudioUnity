using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AudioStudio
{       
    #region Enums
    public enum ObjectType
    {        
        Sound,
        Music,
        Voice,
        Switch,
        Parameter,
        SoundBank,	
        AnimationSound,
        AudioInit,        
        AudioState,
        ButtonSound,
        ColliderSound,
        DropdownSound,
        EmitterSound,
        EffectSound,                
        MenuSound,
        PeriodSound,
        ScrollSound, 
        SetSwitch,
        SliderSound,        
        TimelineSound,
        ToggleSound        
    }

    [Flags]
    public enum MessageType
    {    
        None = 0,
        All = ~0,    
        Notification = 0x1,
        Warning = 0x2,
        Error = 0x4,
        Component = 0x8
    }

    public enum AudioAction
    {
        PostEvent,
        Play,
        End,
        StopEvent,        
        Pause,
        Resume,
        Mute,
        Unmute,
        Load,
        Unload,
        SetValue,
        GetValue,
        SetQueue,
        Stinger,
        TransitionEnter,
        TransitionExit,
        SequenceEnter,
        SequenceExit,
        PreEntry,
        Loop,
        PostExit,
        VoiceLimit,
        Download,
        Activate,
        Deactivate
    }
    #endregion
    
#if UNITY_EDITOR
    public class AudioProfiler : EditorWindow
    {                
        private Vector2 _scrollPosition;
        public static AudioProfiler Instance;

        private class AudioLog
        {
            public MessageType MessageType;
            public string Time;
            public ObjectType ObjectType;
            public AudioAction Action;
            public string ObjectName;
            public string GameObject;
            public string Message;						
        }

        private bool _paused;
        private bool _autoScroll = true;
        
        private bool _includeSound = true;
        private bool _includeMusic = true;
        private bool _includeVoice = true;
        private bool _includeSwitch = true;
        private bool _includeParameter = true;
        private bool _includeBank = true;      
        
        private bool _includeNotification = true;
        private bool _includeWarning = true;
        private bool _includeError = true;
        private bool _includeComponent = true;
        
        private bool _includePostStop = true;
        private bool _includePlayEnd = true;        
        private bool _includeLoadUnload = true;
        
		
        private readonly Queue<AudioLog> _audioLogs = new Queue<AudioLog>();

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (_audioLogs.Count > 200)
                _audioLogs.Dequeue();
        }       
        
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Filter", EditorStyles.boldLabel);
            if (GUILayout.Button("Select All", GUILayout.Width(100))) SelectAll(true);
            if (GUILayout.Button("Deselect All", GUILayout.Width(100))) SelectAll(false);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            _includeComponent = GUILayout.Toggle(_includeComponent, "Components", GUILayout.Width(100));
            _includeNotification = GUILayout.Toggle(_includeNotification, "Notification", GUILayout.Width(100));            
            _includeWarning = GUILayout.Toggle(_includeWarning, "Warning", GUILayout.Width(100));          
            _includeError = GUILayout.Toggle(_includeError, "Error", GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();  
            _includePostStop = GUILayout.Toggle(_includePostStop, "Post/Stop", GUILayout.Width(100));
            _includePlayEnd = GUILayout.Toggle(_includePlayEnd, "Play/Loop/End", GUILayout.Width(100));            
            _includeLoadUnload = GUILayout.Toggle(_includeLoadUnload, "Load/Unload", GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();            
            _includeSound = GUILayout.Toggle(_includeSound, "Sound", GUILayout.Width(100));
            _includeMusic = GUILayout.Toggle(_includeMusic, "Music", GUILayout.Width(100));
            _includeVoice = GUILayout.Toggle(_includeVoice, "Voice", GUILayout.Width(100));
            _includeBank = GUILayout.Toggle(_includeBank, "SoundBank", GUILayout.Width(100));
            _includeSwitch = GUILayout.Toggle(_includeSwitch, "Switch", GUILayout.Width(100));
            _includeParameter = GUILayout.Toggle(_includeParameter, "Parameter", GUILayout.Width(100));            
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Global Voice Count:", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sound: " + SoundClipInstance.GlobalSoundCount, GUILayout.Width(120));
            EditorGUILayout.LabelField("Music: " + MusicTrackInstance.GlobalMusicCount, GUILayout.Width(120));
            EditorGUILayout.LabelField("Voice: " + VoiceEventInstance.GlobalVoiceCount, GUILayout.Width(120));
            EditorGUILayout.LabelField("Bank: " + SoundBank.GlobalBankCount, GUILayout.Width(120));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause")) _paused = true;
            if (GUILayout.Button("Resume")) _paused = false;
            if (GUILayout.Button("Clear")) _audioLogs.Clear();
            _autoScroll =  GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100));
            GUILayout.EndHorizontal();                        
						
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Severity", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Time", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField("GameObject", EditorStyles.boldLabel, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);				
                GUILayout.EndHorizontal();
                
                _scrollPosition =  EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height - 160));
                if (_autoScroll && Application.isPlaying && !EditorApplication.isPaused) _scrollPosition.y = Mathf.Infinity;
                foreach (var audioLog in _audioLogs)
                {                                                            
                    if (!FilterLog(audioLog)) continue;
                    
                    switch (audioLog.MessageType)
                    {
                        case MessageType.Error:
                            GUI.color = Color.red;
                            break;
                        case MessageType.Warning:
                            GUI.color = Color.yellow;
                            break;
                        case MessageType.Component:
                            GUI.color = Color.green;
                            break;
                    }
                                        
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(audioLog.MessageType.ToString(), GUILayout.Width(96));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField(audioLog.Time, GUILayout.Width(80));
                    EditorGUILayout.LabelField(audioLog.ObjectType.ToString(), GUILayout.Width(100));                    
                    DrawAction(audioLog.Action);                    
                    EditorGUILayout.LabelField(audioLog.ObjectName, GUILayout.MinWidth(180));
                    EditorGUILayout.LabelField(audioLog.GameObject, GUILayout.MinWidth(120));
                    EditorGUILayout.LabelField(audioLog.Message);					
                    GUILayout.EndHorizontal();	                    
                }
                GUILayout.EndScrollView();
            }
        }
        
        private bool FilterLog(AudioLog audioLog)
        {            
            switch (audioLog.MessageType)
            {
                case MessageType.Notification: 
                    if (!_includeNotification) return false; break;                                        
                case MessageType.Warning: 
                    if (!_includeWarning) return false; break;                                        
                case MessageType.Error: 
                    if (!_includeError) return false; break;                                        
                case MessageType.Component: 
                    if (!_includeComponent) return false; break;                                        
            }

            switch (audioLog.ObjectType)
            {
                case ObjectType.Music:
                    if (!_includeMusic) return false; break;
                case ObjectType.Sound:
                    if (!_includeSound) return false; break;
                case ObjectType.Voice:
                    if (!_includeVoice) return false; break;
                case ObjectType.SoundBank:
                    if (!_includeBank) return false; break;
                case ObjectType.Switch:                
                    if (!_includeSwitch) return false; break;
                case ObjectType.Parameter:
                    if (!_includeParameter) return false; break;
            }

            switch (audioLog.Action)
            {
                case AudioAction.Play:
                case AudioAction.Loop:    
                case AudioAction.End:
                    if (!_includePlayEnd) return false; break;
                case AudioAction.Load:
                case AudioAction.Unload:
                    if (!_includeLoadUnload) return false; break;
                case AudioAction.PostEvent:
                case AudioAction.StopEvent:
                    if (!_includePostStop) return false; break;
            }
            return true;
        }
        
        private static readonly Color Grey = new Color(0.67f, 0.67f, 0.67f);
        private static readonly Color Skin = new Color(1f, 0.67f, 0.67f);
        private static readonly Color Orange = new Color(1f, 0.5f, 0f);
        private static readonly Color Pink = new Color(1f, 0.67f, 1f);
        private static readonly Color Rose = new Color(1f, 0.33f, 0.67f);       
        private static readonly Color Aqua = new Color(0.5f, 1f, 1f);
        private static readonly Color LightGreen = new Color(0.5f, 1f, 0f);   
        private static readonly Color DarkGreen = new Color(0f, 0.67f, 0.33f);        
        private static readonly Color Purple = new Color(0.67f, 0.5f, 1f);
        private static readonly Color Blue = new Color(0.33f, 0.67f, 1f);                      
        
        private void DrawAction(AudioAction action)
        {
            switch (action)
            {
                case AudioAction.PostEvent:
                    GUI.color = Color.yellow;
                    break;
                case AudioAction.StopEvent:
                    GUI.color = Orange;
                    break;
                case AudioAction.Load:
                case AudioAction.Download:
                    GUI.color = Aqua;
                    break;
                case AudioAction.Unload:
                    GUI.color = Blue;
                    break; 
                case AudioAction.Play:
                case AudioAction.Resume: 
                case AudioAction.Unmute: 
                    GUI.color = LightGreen;
                    break;
                case AudioAction.End:
                case AudioAction.Mute:                                                
                case AudioAction.Pause: 
                    GUI.color = DarkGreen;
                    break;                
                case AudioAction.Activate:                    
                    GUI.color = Pink;                            
                    break;                                                                                                                                                                   
                case AudioAction.Deactivate:
                    GUI.color = Purple;
                    break;
                case AudioAction.SetValue:
                case AudioAction.Stinger:
                case AudioAction.SetQueue:
                case AudioAction.PreEntry:
                case AudioAction.TransitionEnter:
                case AudioAction.SequenceEnter:    
                    GUI.color = Rose;
                    break;
                case AudioAction.GetValue:
                case AudioAction.PostExit:
                case AudioAction.TransitionExit:
                case AudioAction.SequenceExit:
                    GUI.color = Skin;
                    break;
                case AudioAction.VoiceLimit:
                    GUI.color = Grey;
                    break;				                                    
            }
            EditorGUILayout.LabelField(action.ToString(), GUILayout.Width(100));
            GUI.color = Color.white;
        }
        
        private void SelectAll(bool enabled)
        {
            _includeSound = _includeMusic = _includeVoice = _includeBank = _includeSwitch = _includeParameter = 
            _includeError = _includeNotification = _includeWarning = _includePostStop = _includePlayEnd = _includeLoadUnload = enabled;
        }
        
        public void AddLog(MessageType messageType, ObjectType objectType, AudioAction action, string objectName,
            string gameObject, string message, string time)
        {
            if (_paused) return;            
            var newLog = new AudioLog{MessageType = messageType, Time = time, ObjectType = objectType, Action = action, ObjectName = objectName, GameObject = gameObject, Message = message};
            if (FilterLog(newLog)) 
                _audioLogs.Enqueue(newLog);       			
            Repaint();
        }
    }
#endif
}