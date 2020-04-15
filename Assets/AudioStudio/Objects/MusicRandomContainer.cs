using UnityEngine;
using Random = UnityEngine.Random;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Music Random Container", menuName = "AudioStudio/Music/Random Container")]
    public class MusicRandomContainer : MusicContainer
    {
        private byte _lastPlayedIndex = 255;
        public bool AvoidRepeat = true;
        public bool RandomOnLoop;
        
        #region Playback         
        public override MusicContainer GetEvent()
        {
            if (ChildEvents.Count < 2)
                return ChildEvents[0];
            var selectedIndex = Random.Range(0, ChildEvents.Count);
            if (!AvoidRepeat) 
                return ChildEvents[selectedIndex];
            while (selectedIndex == _lastPlayedIndex)
            {
                selectedIndex = Random.Range(0, ChildEvents.Count);
            }
            _lastPlayedIndex = (byte)selectedIndex;
            return ChildEvents[selectedIndex];
        }
        #endregion
    }
}