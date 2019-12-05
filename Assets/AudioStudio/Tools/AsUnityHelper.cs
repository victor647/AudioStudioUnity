using UnityEngine;

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
#endif

namespace AudioStudio.Tools
{
    #region ProfilerEnums
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
        Language
    }

    public enum Severity
    {
        Notification,
        Warning,
        Error,
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
        LoadBank,
        MenuSound,
        ScrollSound,
        SetSwitch,
        SliderSound,
        TimelineSound,
        ToggleSound,
        AudioTimelineClip
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
        Activate,
        Deactivate
    }
    
    public struct ProfilerMessage
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

        #region OfflinePlayback
#if UNITY_EDITOR
        public static void PlayAudioClipOffline(AudioClip clip)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(AudioClip)}, null);
            method?.Invoke(null, new object[] {clip});
        }

        public static void StopAudioClipOffline(AudioClip clip)
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtilClass.GetMethod("StopClip", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(AudioClip)}, null);
            method?.Invoke(null, new object[] {clip});
        }
#endif
        #endregion


        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        #region Profiler
#if UNITY_EDITOR
        public static Action<ProfilerMessage> ProfilerCallback;
#endif
        
        public static void DebugToProfiler(Severity severity, AudioObjectType objectType, AudioAction action, AudioTriggerSource triggerFrom, 
            string eventName, GameObject gameObject = null, string message = "")
        {
#if UNITY_EDITOR
            var newMessage = new ProfilerMessage
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
            if (ProfilerCallback != null) 
                ProfilerCallback.Invoke(newMessage);
#else
            if (severity == Severity.Error && Debug.unityLogger.logEnabled)
            {
                var log = string.Format("AudioError: {0}_{1}\tName: {2}\tTrigger: {3}\tGameObject: {4}\tMessage: {5}", objectType, action, eventName, triggerFrom, gameObject ? gameObject.name : "Global Audio Emitter", message);
                Debug.LogError(log);
            }    
#endif
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