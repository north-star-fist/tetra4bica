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

    public class PlayerSfx : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("playerDeathSfx")]
        private AudioResource _playerDeathSfx;
        [SerializeField, FormerlySerializedAs("playerShotSfx")]
        private AudioResource _playerShotSfx;
        [SerializeField, FormerlySerializedAs("playerRotateSfx")]
        private AudioResource _playerRotateSfx;

        [Inject]
        private IGameEvents _gameLogic;
        [Inject]
        private IAudioSourceManager _audioManager;

        private AudioSource _playerAudioSource;


        private void Start()
        {
            Setup(
                _gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default),
                _gameLogic.ShotStream,
                _gameLogic.RotationStream
            );
            _playerAudioSource = _audioManager.GetAudioSource(AudioSourceId.SoundEffects);
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
