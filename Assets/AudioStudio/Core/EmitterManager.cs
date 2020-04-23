using System.Collections.Generic;
using AudioStudio.Configs;

namespace AudioStudio
{
    public static class EmitterManager
    {
        private static readonly List<SoundClipInstance> _globalSoundInstances = new List<SoundClipInstance>();
        private static readonly List<MusicTrackInstance> _globalMusicInstances = new List<MusicTrackInstance>();
        private static readonly List<VoiceEventInstance> _globalVoiceInstances = new List<VoiceEventInstance>();
        private static readonly List<AudioSwitchInstance> _globalSwitchInstances = new List<AudioSwitchInstance>();
        private static readonly List<AudioParameterInstance> _globalParameterInstances = new List<AudioParameterInstance>();

        public static List<string> GetSoundInstances()
        {
            var list = new List<string>();
            foreach (var instance in _globalSoundInstances)
            {
                list.Add(instance.SoundClip.name +  " @ " + instance.gameObject.name);
            }
            return list;
        }
        
        public static List<string> GetMusicInstances()
        {
            var list = new List<string>();
            foreach (var instance in _globalMusicInstances)
            {
                list.Add(instance.MusicTrack.name);
            }
            return list;
        }
        
        public static List<string> GetVoiceInstances()
        {
            var list = new List<string>();
            foreach (var instance in _globalVoiceInstances)
            {
                list.Add(instance.VoiceEvent.name +  " @ " + instance.gameObject.name);
            }
            return list;
        }
        
        public static List<string> GetSwitchInstances()
        {
            var list = new List<string>();
            foreach (var instance in _globalSwitchInstances)
            {
                list.Add(instance.AudioSwitch.name +  " @ " + instance.gameObject.name);
            }
            return list;
        }
        
        public static List<string> GetParameterInstances()
        {
            var list = new List<string>();
            foreach (var instance in _globalParameterInstances)
            {
                list.Add(instance.Parameter.name +  " @ " + instance.gameObject.name);
            }
            return list;
        }

        internal static void AddSoundInstance(SoundClipInstance instance)
        {
            _globalSoundInstances.Add(instance);
        }

        internal static void RemoveSoundInstance(SoundClipInstance instance)
        {
            _globalSoundInstances.Remove(instance);
        }
        
        internal static void AddMusicInstance(MusicTrackInstance instance)
        {
            _globalMusicInstances.Add(instance);
        }
        
        internal static void RemoveMusicInstance(MusicTrackInstance instance)
        {
            _globalMusicInstances.Remove(instance);
        }
        
        internal static void AddVoiceInstance(VoiceEventInstance instance)
        {
            _globalVoiceInstances.Add(instance);
        }
        
        internal static void RemoveVoiceInstance(VoiceEventInstance instance)
        {
            _globalVoiceInstances.Remove(instance);
        }

        internal static void AddSwitchInstance(AudioSwitchInstance instance)
        {
            _globalSwitchInstances.Add(instance);
        }
        
        internal static void RemoveSwitchInstance(AudioSwitchInstance instance)
        {
            _globalSwitchInstances.Remove(instance);
        }
        
        internal static void AddParameterInstance(AudioParameterInstance instance)
        {
            _globalParameterInstances.Add(instance);
        }
        
        internal static void RemoveParameterInstance(AudioParameterInstance instance)
        {
            _globalParameterInstances.Remove(instance);
        }

        // called each frame to check if audio ends
        internal static void UpdateAudioInstances()
        {
            foreach (var instance in _globalSoundInstances)
            {
                instance.UpdatePlayingStatus();
            }
            
            foreach (var instance in _globalVoiceInstances)
            {
                instance.UpdatePlayingStatus();
            }
            
            if (_globalMusicInstances.Count > 0)
                MusicTransport.Instance.UpdateMusicPlayback();
            
            foreach (var instance in _globalSwitchInstances)
            {
                instance.CheckPendingSwitch();
            }
            
            foreach (var instance in _globalParameterInstances)
            {
                instance.UpdateSlewValues();
            }
        }
    }
}
