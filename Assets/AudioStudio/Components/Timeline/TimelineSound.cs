﻿using UnityEngine;
using UnityEngine.Playables;

namespace AudioStudio.Components
{
    [AddComponentMenu("AudioStudio/TimelineSound")]
    [DisallowMultipleComponent]
    public class TimelineSound : AudioEmitter3D
    {                        
        public GameObject[] Emitters = new GameObject[0];

        public override bool IsValid()
        {
            return GetComponent<PlayableDirector>() != null;
        }
    }
}