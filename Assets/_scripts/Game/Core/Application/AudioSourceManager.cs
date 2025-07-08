using System.Collections.Generic;
using UnityEngine;

namespace Tetra4bica.Init
{
    public class AudioSourceManager : IAudioSourceManager
    {
        private readonly Dictionary<AudioSourceId, AudioSource> _map = new();

        public AudioSourceManager(Dictionary<AudioSourceId, AudioSource> map)
        {
            _map = map;
        }

        public AudioSource GetAudioSource(AudioSourceId id) => _map.TryGetValue(id, out var aSource) ? aSource : null;
    }
}
