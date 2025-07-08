using UnityEngine;

namespace Tetra4bica.Init
{
    public interface IAudioSourceManager
    {
        public AudioSource GetAudioSource(AudioSourceId audioSourceId);
    }
}
