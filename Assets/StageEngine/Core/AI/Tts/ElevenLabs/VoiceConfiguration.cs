using System;
using UnityEngine;

namespace StageEngine.Core.AI.Tts.ElevenLabs
{
    [Serializable]
    public class VoiceConfiguration
    {
        public string name = "New Voice";
        public Gender gender;
        public string voiceId;
        [Range(0f, 1f)]
        public float volume;
        [Range(0.7f, 1.2f)]
        public float speed;
        [Range(0f, 1f)]
        public float stability;
        [Range(0f, 1f)]
        public float similarity;
        [Range(0f, 1f)]
        public float style;
        public bool useSpeakerBoost;
        public bool directorOnly;

        public enum Gender
        {
            Male,
            Female,
        }
    }
}
