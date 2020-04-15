using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Music Sequence Container", menuName = "AudioStudio/Music/Sequence Container")]
    public class MusicSequenceContainer : MusicContainer
    {
        #region Fields
        public bool SequenceByStep;
        public bool LoopEntireSequence;
        private byte _lastPlayedIndex = 255;
        #endregion

        #region Playback         
        public override MusicContainer GetEvent()
        {
            if (SequenceByStep)
            {
                _lastPlayedIndex++;
                if (_lastPlayedIndex == ChildEvents.Count)
                    _lastPlayedIndex = 0;
            }
            else
                _lastPlayedIndex = 0;
            return ChildEvents[_lastPlayedIndex];
        }

        public MusicContainer GetNextEvent()
        {
            _lastPlayedIndex++;
            if (_lastPlayedIndex == ChildEvents.Count)
            {
                _lastPlayedIndex = 0;
                return LoopEntireSequence ? ChildEvents[0] : null;
            }
            return ChildEvents[_lastPlayedIndex];
        }
        #endregion
    }
}