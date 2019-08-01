using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Transition Segment", menuName = "Audio/Music/Transition Segment")]
    public class MusicTransitionSegment : MusicTrack
    {
        public MusicContainer Origin;
        public MusicContainer Destination;
        public float OriginFadeOutTime;
        public float SegmentFadeInTime;
        public float SegmentFadeOutTime;
        public float DestinationFadeInTime;

        private void OnEnable()
        {
            IndependentEvent = true;
        }

        public override void OnPlay()
        {            
            MusicTransport.Instance.SetMusicQueue(Destination, SegmentFadeOutTime, DestinationFadeInTime);
        }
    }
}