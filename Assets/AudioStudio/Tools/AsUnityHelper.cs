using UnityEngine;
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
#endif

namespace AudioStudio.Tools
{
    #region ConsoleEnums
    public enum AudioObjectType
    {        
        SFX,
        Music,
        Voice,
        Instrument,
        Switch,
        AudioState,
        Parameter,
        SoundBank,
        Listener,
        Language,
        AudioMixer,
        Component
    }

    public enum Severity
    {
        Notification,
        Warning,
        Error,
        None
    }
    
    public enum AudioTriggerSource
    {
        Code,
        InspectorAudition,
        Initialization,
        AnimationSound,
        AudioListener3D,
        AudioState,
        ButtonSound,
        ColliderSound,
        DropdownSound,
        EffectSound,
        EmitterSound,
        EventSound,
        LoadBank,
        MenuSound,
        SimpleAudioPlayer,
        ScrollSound,
        SetSwitch,
        SliderSound,
        TimelineSound,
        ToggleSound,
        AudioTimelineClip
    }

    public enum AudioAction
    {
        Play,
        End,
        Stop,        
        Pause,
        Resume,
        Mute,
        UnMute,
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
        Activate,
        Deactivate
    }
    
#if UNITY_EDITOR
    public struct AudioConsoleMessage
    {
        public Severity Severity;
        public string Time;
        public AudioObjectType ObjectType;
        public AudioAction Action;
        public AudioTriggerSource TriggerFrom;
        public string ObjectName;
        public GameObject GameObject;
        public string GameObjectName;
        public string Message;
    }
#endif  
    #endregion
    
    #region Midi
    public class MidiMessage
    {
        public readonly uint Device;
        public readonly byte StatusByte;
        public readonly byte DataByte1;
        public readonly byte DataByte2;

        public int Channel => (StatusByte & 0xf) + 1;
        public int StatusCode => StatusByte >> 4;
        public int PitchBendValue => (DataByte2 << 7 | DataByte1) - 8192;

        public MidiMessage(ulong data)
        {
            Device = (uint) (data & 0xffffffffUL);
            StatusByte = (byte) ((data >> 32) & 0xff);
            DataByte1 = (byte) ((data >> 40) & 0xff);
            DataByte2 = (byte) ((data >> 48) & 0xff);
        }
    }
    #endregion
    
    public static class AsUnityHelper
    {
        public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
#if UNITY_EDITOR
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
#else
            var asset = ScriptableObject.CreateInstance<T>();
            Debug.LogWarning(typeof(T).Name + " config not found, creating an empty one instead");
#endif
            return asset;
        }


        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        #region AudioConsole
        public static Severity DebugLogLevel = Severity.Error;
#if UNITY_EDITOR
        public static Action<AudioConsoleMessage> AudioConsoleCallback;
#endif
        
        public static void AddLogEntry(Severity severity, AudioObjectType objectType, AudioAction action, AudioTriggerSource triggerFrom, string eventName, GameObject gameObject = null, string message = "")
        {
#if UNITY_EDITOR
            if (AudioConsoleCallback != null)
            {
                var newMessage = new AudioConsoleMessage
                {
                    Severity = severity,
                    Time = Time.time.ToString("0.000"),
                    ObjectType = objectType,
                    Action = action,
                    TriggerFrom = triggerFrom,
                    ObjectName = eventName,
                    GameObject = gameObject,
                    GameObjectName = gameObject ? gameObject.name : "Global Audio Emitter",
                    Message = message,
                };
                AudioConsoleCallback.Invoke(newMessage);
                return;
            }
#endif
            if (severity >= DebugLogLevel && Debug.unityLogger.logEnabled)
            {
                var log = string.Format("Audio{0}: {1}_{2}\tName: {3}\tTrigger: {4}\tGameObject: {5}\tMessage: {6}", severity, objectType, action, eventName, triggerFrom, gameObject ? gameObject.name : "Global Audio Emitter", message);
                switch (severity)
                {
                    case Severity.Error:
                        Debug.LogError(log);
                        break;
                    case Severity.Warning:
                        Debug.LogWarning(log);
                        break;
                    case Severity.Notification:
                        Debug.Log(log);
                        break;
                }
            }
        }
        #endregion

        #region Midi
#if UNITY_EDITOR        
        public static Action<MidiMessage> MidiCallback;
#endif
        
        public static void OutputToMidiConsole(MidiMessage message)
        {
#if UNITY_EDITOR
            MidiCallback?.Invoke(message);
#endif            
        }
        #endregion
    }
}