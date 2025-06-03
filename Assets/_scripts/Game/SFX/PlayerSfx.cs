using System;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound
{

    public class PlayerSfx : MonoBehaviour
    {

        [Inject]
        private IGameEvents _gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        private AudioSource _playerAudioSource;

        [SerializeField]
        private AudioResource _playerDeathSfx;
        [SerializeField]
        private AudioResource _playerShotSfx;
        [SerializeField]
        private AudioResource _playerRotateSfx;


        private void Awake()
        {
            Setup(
                _gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default),
                _gameLogic.ShotStream,
                _gameLogic.RotationStream
            );
        }

        void Setup(
            IObservable<Unit> gameOverStream,
            IObservable<Vector2Int> playerShotStream,
            IObservable<bool> rotationStream)
        {
            gameOverStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerDeathSfx)
            );
            playerShotStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerShotSfx)
            );
            rotationStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerRotateSfx)
            );
        }
    }
}
