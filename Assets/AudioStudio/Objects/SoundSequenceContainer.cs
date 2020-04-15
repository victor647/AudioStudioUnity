using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Sequence Container", menuName = "AudioStudio/Sound/Sequence Container")]
    public class SoundSequenceContainer : SoundContainer
    {
        private byte _lastPlayedIndex = 255;
        
        internal override SoundContainer GetChildByPlayLogic(GameObject soundSource)
        {
            _lastPlayedIndex++;
            if (_lastPlayedIndex == ChildEvents.Count) _lastPlayedIndex = 0;
            return ChildEvents[_lastPlayedIndex];
        }
    }
}