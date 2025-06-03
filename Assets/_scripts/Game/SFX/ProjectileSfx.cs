using System;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound
{

    public class ProjectileSfx : MonoBehaviour
    {

        [Inject]
        IGameEvents _gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource _audioSource;
        [SerializeField]
        private AudioResource _particleFrozenSfx;

        private void Awake()
        {
            Setup(_gameLogic.FrozenProjectilesStream);
        }


        void Setup(IObservable<Vector2Int> projectileFrozenStream)
        {
            projectileFrozenStream.Subscribe(_ => SoundUtils.PlaySound(_audioSource, _particleFrozenSfx));
        }
    }
}
