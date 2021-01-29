using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Random Container", menuName = "AudioStudio/Sound/Random Container")]
    public class SoundRandomContainer : SoundContainer
    {
        public bool AvoidRepeat = true;
        public bool RandomOnLoop;
        public float CrossFadeTime = 0.5f;
        private byte _lastPlayedIndex = 255;
        
        internal override SoundContainer GetChildByPlayLogic(GameObject soundSource)
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
    }
}