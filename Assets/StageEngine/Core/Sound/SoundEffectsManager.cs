using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StageEngine.Core.Conversations;
using StageEngine.Core.Game.Session;
using StageEngine.Core.Utility;
using UnityEngine;

namespace StageEngine.Core.Sound
{
    public abstract class SoundEffectsManager : Singleton<SoundEffectsManager>
    {
        [SerializeField]
        protected List<SoundEffect> soundEffects = new List<SoundEffect>()
        {
            { new SoundEffect
            {
                id = "new-message",
            }},
            { new SoundEffect()
            {
                id = "new-player",
            }},
        };
        public List<SoundEffect> SoundEffects => soundEffects;

        [SerializeField]
        private int defaultPriority = 110;
        [SerializeField]
        private float defaultVolume = 0.8f;

        public virtual void Start()
        {
            Subscribe();
        }
        public virtual void Subscribe()
        {
            ConversationsManager.Instance.OnMessageAdded += m => { PlaySoundEffect("new-message"); };
            SessionManager.Instance.Data.Players.CollectionChanged += (s, e) => { PlaySoundEffect("new-player"); };
        }

        protected virtual void PlaySoundEffect(string id)
        {
            var soundEffect = soundEffects.Where(e => e.id.Equals(id)).First();
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.priority = defaultPriority;
            audioSource.volume = soundEffect.volume == 0 ? defaultVolume : soundEffect.volume;
            audioSource.clip = soundEffect.audio;
            audioSource.Play();
            StartCoroutine(DestroyAudioSourceAfterPlaying(audioSource));
        }

        private IEnumerator DestroyAudioSourceAfterPlaying(AudioSource audioSource)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            Destroy(audioSource);
        }
    }
}
