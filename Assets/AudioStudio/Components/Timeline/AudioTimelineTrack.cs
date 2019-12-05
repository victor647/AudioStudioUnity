using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AudioStudio.Timeline
{
    [TrackColor(1f, 0.5f, 0f)]
    [TrackClipType(typeof(AudioTimelineClip))]
    public class AudioTimelineTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var wwiseClip = clip.asset as AudioTimelineClip;
                if (!wwiseClip) continue;
                wwiseClip.EndTime = clip.end;
                clip.displayName = wwiseClip.AutoRename();
            }
            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}