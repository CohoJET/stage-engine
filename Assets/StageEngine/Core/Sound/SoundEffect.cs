using System;
using UnityEngine;

namespace StageEngine.Core.Sound
{
    [Serializable]
    public class SoundEffect
    {
        public string id;
        public AudioClip audio;
        public float volume;
    }
}
