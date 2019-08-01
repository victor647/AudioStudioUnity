
using AudioStudio.Configs;
#if UNITY_2017_1_OR_NEWER
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;


namespace AudioStudio.Components
{
    [System.Serializable]
    public class AudioPlayableAsset : PlayableAsset
    {
        public AudioEventReference[] StartEvents = new AudioEventReference[0];
        public AudioEventReference[] EndEvents = new AudioEventReference[0];                
        public bool StopOnEnd = true;  
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<TimelineAudioRegion>.Create(graph);
            playable.GetBehaviour().Init(this, go);
            return playable;
        }

        public bool IsValid()
        {
            return StartEvents.Any(s => s.IsValid()) || EndEvents.Any(s => s.IsValid());
        }
        
        public string AutoRename()
        {
            if (StartEvents.Length > 0)
                return StartEvents[0].Name;
            if (EndEvents.Length > 0)
                return EndEvents[0].Name;
            return "Empty";
        }
    }

    public class TimelineAudioRegion : PlayableBehaviour
    {        
        private GameObject _emitter;
        private AudioPlayableAsset _component;        
        
        public void Init(AudioPlayableAsset component, GameObject go)
        {
            _component = component;            
            _emitter = go;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!_emitter || !Application.isPlaying) return;
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.TimelineSound, AudioAction.Activate, "Region Start", _emitter.name);
            foreach (var evt in _component.StartEvents)
            {
                evt.Post(_emitter);
            }    
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {            
            if (!_emitter || !Application.isPlaying) return;
            if (info.deltaTime == 0f && info.effectiveWeight == 0f) return;
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.TimelineSound, AudioAction.Deactivate, "Region End", _emitter.name);
            if (_component.StopOnEnd)
            {
                foreach (var evt in _component.StartEvents)
                {
                    evt.Stop(_emitter);
                }
            }
            foreach (var evt in _component.EndEvents)
            {
                evt.Post(_emitter);
            }                                                
        }
    }
}
#endif