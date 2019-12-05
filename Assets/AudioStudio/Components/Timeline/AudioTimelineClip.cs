using System;
using System.Collections.Generic;
using System.Linq;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Playables;

namespace AudioStudio.Timeline
{
    [Serializable]
    public class AudioTimelineClip : PlayableAsset
    {
        public AudioEventReference[] StartEvents = new AudioEventReference[0];
        public AudioEventReference[] EndEvents = new AudioEventReference[0];
        public bool StopOnEnd = true;
        public SetSwitchReference[] StartSwitches = new SetSwitchReference[0];
        public SetSwitchReference[] EndSwitches = new SetSwitchReference[0];
        public bool GlobalSwitch;
        public int EmitterIndex;
        [NonSerialized]
        public double EndTime;
        
        private TimelineSound _timelineSound;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<AudioTimelineComponent>.Create(graph);
            _timelineSound = AsUnityHelper.GetOrAddComponent<TimelineSound>(go);
            if (EmitterIndex > 0 && _timelineSound.Emitters.Length >= EmitterIndex)
                playable.GetBehaviour().Init(this, _timelineSound.Emitters[EmitterIndex - 1], go);
            else
                playable.GetBehaviour().Init(this, null, go);
            return playable;
        }

        public bool IsValid()
        {
            return StartEvents.Any(s => s.IsValid()) || EndEvents.Any(s => s.IsValid());
        }
        
        public string[] GetEmitterNames()
        {
            var names = new List<string>{"Timeline"};
            if (_timelineSound)
                names.AddRange(_timelineSound.Emitters.Select(emitter => emitter ? emitter.name : "Emitter is Missing!"));
            return names.ToArray();
        }

        public string AutoRename()
        {
            if (StartEvents.Length > 0)
                return StartEvents[0].Name;
            if (EndEvents.Length > 0)
                return EndEvents[0].Name;
            return "Empty Audio Clip";
        }
    }
}