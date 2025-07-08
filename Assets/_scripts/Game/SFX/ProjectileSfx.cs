using System;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Sound
{

    public class ProjectileSfx : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("particleFrozenSfx")]
        private AudioResource _particleFrozenSfx;

        IGameEvents _gameLogic;
        [Inject]
        private IAudioSourceManager _audioManager;

        AudioSource _audioSource;

        private void Start()
        {
            _audioSource = _audioManager.GetAudioSource(AudioSourceId.SoundEffects);
        }


        public void Setup(IObservable<Vector2Int> projectileFrozenStream)
        {
            projectileFrozenStream.Subscribe(_ => SoundUtils.PlaySound(_audioSource, _particleFrozenSfx));
        }
    }
}
