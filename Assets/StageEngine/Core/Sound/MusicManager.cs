using System.Collections.Generic;
using StageEngine.Core.Utility;
using UnityEngine;

namespace StageEngine.Core.Sound
{
    public class MusicManager : Singleton<MusicManager>
    {
        [SerializeField]
        private MusicTrack mainTheme;
        public MusicTrack MainTheme => mainTheme;

        [SerializeField]
        private List<MusicTrack> musicLibrary;
        public List<MusicTrack> MusicLibrary => musicLibrary;

        private AudioSource audioSource;

        public void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void PlayMainTheme()
        {
            audioSource.Stop();
            audioSource.clip = mainTheme.Audio;
            audioSource.Play();
        }
    }
}
