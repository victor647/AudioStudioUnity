using System;
using UnityEngine;

namespace AudioStudio.Configs
{
    [CreateAssetMenu(fileName = "New Sound Blend Container", menuName = "AudioStudio/Sound/Blend Container")]
    public class SoundBlendContainer : SoundContainer
    {
        protected override SoundClip GetChild(GameObject soundSource, float fadeInTime, Action<GameObject> endCallback = null)
        {
            for (var i = 0; i < ChildEvents.Count; i++)
            {
                var evt = ChildEvents[i];
                if (i == 0) //only the first clip will have the end callback
                    evt.Play(soundSource, fadeInTime, endCallback);
                else
                    evt.Play(soundSource, fadeInTime);
            }
            return null;
        }
    }
}