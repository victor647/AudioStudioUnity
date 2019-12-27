﻿using UnityEditor;
using System.Collections.Generic;
using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class AudioProfiler : EditorWindow
    {
        private readonly Queue<ProfilerMessage> _ProfilerMessages = new Queue<ProfilerMessage>();
        private static readonly Dictionary<AudioTriggerSource, bool> _componentInclusions = new Dictionary<AudioTriggerSource, bool>();
        private bool _includeComponents = true;
        private bool _includeCode = true;
        private bool _includeAudition = true;

        private bool _includeNotification = true;
        private bool _includeWarning = true;
        private bool _includeError = true;
        
        private bool _includePlayback = true;

        private bool _includeSound = true;
        private bool _includeMusic = true;
        private bool _includeVoice = true;
        private bool _includeBank = true;
        private bool _includeSwitch = true;
        private bool _includeParameter = true;

        private bool _paused;

        private void AddLog(ProfilerMessage message)
        {
            if (_paused) return;
            if (FilterLog(message)) 
                _ProfilerMessages.Enqueue(message);       			
            Repaint();
        }

        #region Init

        private void OnEnable()
        {
            AsUnityHelper.ProfilerCallback += AddLog;
            RegisterComponents();
        }

        private void RegisterComponents()
        {
            _componentInclusions[AudioTriggerSource.AnimationSound] = true;
            _componentInclusions[AudioTriggerSource.AudioListener3D] = true;
            _componentInclusions[AudioTriggerSource.AudioState] = true;
            _componentInclusions[AudioTriggerSource.ButtonSound] = true;
            _componentInclusions[AudioTriggerSource.ColliderSound] = true;
            _componentInclusions[AudioTriggerSource.DropdownSound] = true;
            _componentInclusions[AudioTriggerSource.EffectSound] = true;
            _componentInclusions[AudioTriggerSource.EmitterSound] = true;
            _componentInclusions[AudioTriggerSource.EventSound] = true;
            _componentInclusions[AudioTriggerSource.LoadBank] = true;
            _componentInclusions[AudioTriggerSource.MenuSound] = true;
            _componentInclusions[AudioTriggerSource.ScrollSound] = true;
            _componentInclusions[AudioTriggerSource.SetSwitch] = true;
            _componentInclusions[AudioTriggerSource.SliderSound] = true;
            _componentInclusions[AudioTriggerSource.TimelineSound] = true;
            _componentInclusions[AudioTriggerSource.ToggleSound] = true;
            _componentInclusions[AudioTriggerSource.AudioTimelineClip] = true;
        }

        private void OnDestroy()
        {
            AsUnityHelper.ProfilerCallback = null;
        }

        private void Update()
        {
            if (_ProfilerMessages.Count > 200)
                _ProfilerMessages.Dequeue();
        }

        #endregion

        #region GUI
        private bool _autoScroll = true;
        private Vector2 _scrollPosition;

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Filter", EditorStyles.boldLabel);
            if (GUILayout.Button("Select All", GUILayout.Width(100))) SelectAll(true);
            if (GUILayout.Button("Deselect All", GUILayout.Width(100))) SelectAll(false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Severity:", GUILayout.Width(80));
            _includeNotification = GUILayout.Toggle(_includeNotification, "Notification", GUILayout.Width(100));
            _includeWarning = GUILayout.Toggle(_includeWarning, "Warning", GUILayout.Width(100));
            _includeError = GUILayout.Toggle(_includeError, "Error", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trigger:", GUILayout.Width(80));
            _includeCode = GUILayout.Toggle(_includeCode, "Code", GUILayout.Width(100));
            _includeComponents = GUILayout.Toggle(_includeComponents, GUIContent.none, GUILayout.Width(10));
            if (GUILayout.Button("Components", GUI.skin.label, GUILayout.Width(86)))
                ProfilerComponentToggle.Init();
            _includeAudition = GUILayout.Toggle(_includeAudition, "Inspector Audition");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(80));
            _includeSound = GUILayout.Toggle(_includeSound, "SFX (" + SoundClip.GlobalSoundCount + ")", GUILayout.Width(100));
            _includeMusic = GUILayout.Toggle(_includeMusic, "Music (" + MusicTrack.GlobalMusicCount + ")", GUILayout.Width(100));
            _includeVoice = GUILayout.Toggle(_includeVoice, "Voice (" + VoiceEvent.GlobalVoiceCount + ")", GUILayout.Width(100));
            _includeBank = GUILayout.Toggle(_includeBank, "Bank (" + SoundBank.GlobalBankCount + ")", GUILayout.Width(100));
            _includeSwitch = GUILayout.Toggle(_includeSwitch, "Switch", GUILayout.Width(100));
            _includeParameter = GUILayout.Toggle(_includeParameter, "Parameter", GUILayout.Width(100));
            _includePlayback = GUILayout.Toggle(_includePlayback, "Playback", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause")) _paused = true;
            if (GUILayout.Button("Resume")) _paused = false;
            if (GUILayout.Button("Clear")) _ProfilerMessages.Clear();
            _autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Severity", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Time", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));
                EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField("GameObject", EditorStyles.boldLabel, GUILayout.MinWidth(120), GUILayout.MaxWidth(200));
                EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height - 110));
                if (_autoScroll && Application.isPlaying && !EditorApplication.isPaused) _scrollPosition.y = Mathf.Infinity;
                foreach (var ProfilerMessage in _ProfilerMessages)
                {
                    if (!FilterLog(ProfilerMessage)) continue;
                    switch (ProfilerMessage.Severity)
                    {
                        case Severity.Error:
                            GUI.color = Color.red;
                            break;
                        case Severity.Warning:
                            GUI.color = Color.yellow;
                            break;
                    }

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(ProfilerMessage.Severity.ToString(), GUILayout.Width(96));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField(ProfilerMessage.Time, GUILayout.Width(80));
                    DrawObject(ProfilerMessage.ObjectType);
                    DrawAction(ProfilerMessage.Action);
                    EditorGUILayout.LabelField(ProfilerMessage.ObjectName, GUILayout.MinWidth(150), GUILayout.MaxWidth(300));
                    DrawTrigger(ProfilerMessage.TriggerFrom);
                    if (GUILayout.Button(ProfilerMessage.GameObjectName, GUI.skin.label, GUILayout.MinWidth(120), GUILayout.MaxWidth(200)))
                        Selection.activeGameObject = ProfilerMessage.GameObject;
                    EditorGUILayout.LabelField(ProfilerMessage.Message);
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
        }

        private bool FilterLog(ProfilerMessage ProfilerMessage)
        {
            switch (ProfilerMessage.Severity)
            {
                case Severity.Notification:
                    if (!_includeNotification) return false;
                    break;
                case Severity.Warning:
                    if (!_includeWarning) return false;
                    break;
                case Severity.Error:
                    if (!_includeError) return false;
                    break;
            }

            switch (ProfilerMessage.ObjectType)
            {
                case AudioObjectType.Music:
                    if (!_includeMusic) return false;
                    break;
                case AudioObjectType.SFX:
                    if (!_includeSound) return false;
                    break;
                case AudioObjectType.Voice:
                    if (!_includeVoice) return false;
                    break;
                case AudioObjectType.SoundBank:
                    if (!_includeBank) return false;
                    break;
                case AudioObjectType.Switch:
                    if (!_includeSwitch) return false;
                    break;
                case AudioObjectType.Parameter:
                    if (!_includeParameter) return false;
                    break;
            }

            switch (ProfilerMessage.Action)
            {
                case AudioAction.End:
                case AudioAction.Loop:
                case AudioAction.Pause:
                case AudioAction.Resume:
                case AudioAction.PreEntry:
                case AudioAction.PostExit:
                case AudioAction.SequenceEnter:
                case AudioAction.SequenceExit:
                case AudioAction.TransitionEnter:
                case AudioAction.TransitionExit:
                case AudioAction.SetQueue:
                    if (!_includePlayback) return false;
                    break;
            }

            switch (ProfilerMessage.TriggerFrom)
            {
                case AudioTriggerSource.Code:
                    if (!_includeCode) return false;
                    break;
                case AudioTriggerSource.InspectorAudition:
                    if (!_includeAudition) return false;
                    break;
                case AudioTriggerSource.Initialization:
                    return true;
                default:
                    if (!_includeComponents || !_componentInclusions[ProfilerMessage.TriggerFrom]) return false;
                    break;
            }
            return true;
        }

        private static readonly Color Yellow = new Color(1f, 0.9f, 0.6f);
        private static readonly Color Skin = new Color(1f, 0.67f, 0.67f);
        private static readonly Color Orange = new Color(1f, 0.5f, 0f);
        private static readonly Color Pink = new Color(1f, 0.67f, 1f);
        private static readonly Color Rose = new Color(1f, 0.33f, 0.67f);
        private static readonly Color Aqua = new Color(0.5f, 1f, 1f);
        private static readonly Color LightGreen = new Color(0.5f, 1f, 0f);
        private static readonly Color DarkGreen = new Color(0f, 0.67f, 0.33f);
        private static readonly Color Purple = new Color(0.67f, 0.5f, 1f);
        private static readonly Color Blue = new Color(0.33f, 0.67f, 1f);
        private static readonly Color Grey = new Color(0.7f, 0.7f, 0.7f);

        private void DrawObject(AudioObjectType type)
        {
            switch (type)
            {
                case AudioObjectType.SFX:
                    GUI.color = Aqua;
                    break;
                case AudioObjectType.Music:
                case AudioObjectType.Instrument:    
                    GUI.color = LightGreen;
                    break;
                case AudioObjectType.Voice:
                case AudioObjectType.Language:
                    GUI.color = Yellow;
                    break;
                case AudioObjectType.AudioState:
                case AudioObjectType.Switch:
                case AudioObjectType.Parameter: 
                    GUI.color = Pink;
                    break;
                case AudioObjectType.SoundBank:
                    GUI.color = Blue;
                    break;
            }
            EditorGUILayout.LabelField(type.ToString(), GUILayout.Width(100));
            GUI.color = Color.white;
        }
        
        private void DrawAction(AudioAction action)
        {
            switch (action)
            {
                case AudioAction.Play:
                    GUI.color = Skin;
                    break;
                case AudioAction.Stop:
                    GUI.color = Orange;
                    break;
                case AudioAction.TransitionEnter:
                case AudioAction.SequenceEnter:
                case AudioAction.SetQueue:
                case AudioAction.PreEntry:
                case AudioAction.Loop:
                    GUI.color = LightGreen;
                    break;
                case AudioAction.End:   
                case AudioAction.TransitionExit:
                case AudioAction.SequenceExit:
                case AudioAction.PostExit:
                    GUI.color = DarkGreen;
                    break;
                case AudioAction.Load:
                    GUI.color = Aqua;
                    break;
                case AudioAction.Unload:
                    GUI.color = Blue;
                    break;
                case AudioAction.Activate:
                    GUI.color = Pink;
                    break;
                case AudioAction.Deactivate:
                    GUI.color = Purple;
                    break;
                case AudioAction.Mute:
                case AudioAction.Pause:
                    GUI.color = Grey;
                    break;
                case AudioAction.SetValue:
                case AudioAction.GetValue:
                    GUI.color = Rose;
                    break;
            }
            EditorGUILayout.LabelField(action.ToString(), GUILayout.Width(100));
            GUI.color = Color.white;
        }

        private void DrawTrigger(AudioTriggerSource trigger)
        {
            switch (trigger)
            {
                case AudioTriggerSource.Code:
                    GUI.color = Orange;
                    break;
                case AudioTriggerSource.Initialization:
                    GUI.color = Pink;
                    break;
            }
            EditorGUILayout.LabelField(trigger.ToString(), GUILayout.Width(100));
            GUI.color = Color.white;
        }

        private void SelectAll(bool enabled)
        {
            _includeSound = _includeMusic = _includeVoice = _includeBank = _includeSwitch = _includeParameter = _includeError = 
                _includeNotification = _includeWarning = _includeComponents = _includeCode = _includeAudition = _includePlayback = enabled;
        }

        #endregion

        private class ProfilerComponentToggle : EditorWindow
        {
            public static void Init()
            {					
                var position = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                position.y += 10;
                var window = CreateInstance<ProfilerComponentToggle>();
                window.ShowAsDropDown(new Rect(position, Vector2.zero), new Vector2(130, 350));
            }

            private void OnGUI()
            {
                var selections = new Dictionary<AudioTriggerSource, bool>(_componentInclusions);
                foreach (var component in selections)
                {
                    _componentInclusions[component.Key] = GUILayout.Toggle(component.Value, component.Key.ToString());
                }					
            }
        }
    }
}