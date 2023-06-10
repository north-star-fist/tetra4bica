using Sergei.Safonov.Audio;
using System;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound {

    public class PlayerSfx : MonoBehaviour {

        [Inject]
        IGameEvents gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource playerAudioSource;

        public AudioResource playerDeathSfx;
        public AudioResource playerShotSfx;
        public AudioResource playerRotateSfx;


        private void Awake() {
            Setup(
                gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default),
                gameLogic.ShotStream,
                gameLogic.RotationStream
            );
        }

        void Setup(
            IObservable<Unit> gameOverStream,
            IObservable<Vector2Int> playerShotStream,
            IObservable<bool> rotationStream) {
            gameOverStream.Subscribe(
                _ => SoundUtils.PlaySound(playerAudioSource, playerDeathSfx)
            );
            playerShotStream.Subscribe(
                _ => SoundUtils.PlaySound(playerAudioSource, playerShotSfx)
            );
            rotationStream.Subscribe(
                _ => SoundUtils.PlaySound(playerAudioSource, playerRotateSfx)
            );
        }
    }
}