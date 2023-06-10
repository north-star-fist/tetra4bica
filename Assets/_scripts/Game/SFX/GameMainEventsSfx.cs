using Sergei.Safonov.Audio;
using System;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound {

    public class GameMainEventsSfx : MonoBehaviour {

        [Inject]
        IGameEvents gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource sfxAudioSource;

        public AudioResource gameStartSfx;
        public AudioResource gameOverSfx;

        public AudioSource gameplayBgmAudioSource;
        public AudioSource gameOverBgmAudioSource;

        private void Awake() {
            Setup(
                gameLogic.GamePhaseStream.Scan
                (
                    // Getting switching to Started phase only after NotStarted or GameOver phases
                    // keeping in mind that the Game can not be started at Paused state (if it can -
                    // game starting sound is played)
                    (GamePhase.NotStarted, GamePhase.NotStarted),
                    (phaseSwitch, newPhase) => {
                        return (phaseSwitch.Item2, newPhase);
                    }
                ).Where(phaseSwitch => phaseSwitch.Item1 is GamePhase.GameOver or GamePhase.NotStarted
                    && phaseSwitch.Item2 is GamePhase.Started).Select(_ => Unit.Default),
                gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            );
        }

        void Setup(IObservable<Unit> gameStartedStream, IObservable<Unit> gameOverStream) {
            gameStartedStream.Subscribe(_ => startBgmAfterSfx(gameStartSfx, gameplayBgmAudioSource));
            gameOverStream.Subscribe(_ => startBgmAfterSfx(gameOverSfx, gameOverBgmAudioSource));
        }

        private void startBgmAfterSfx(AudioResource sfx, AudioSource bgmAudioSource) {
            gameOverBgmAudioSource.Stop();
            gameplayBgmAudioSource.Stop();
            SoundUtils.PlaySound(sfxAudioSource, sfx);
            double bgmDelaySeconds = sfxAudioSource.clip.length;
            bgmAudioSource.PlayScheduled(AudioSettings.dspTime + bgmDelaySeconds);
        }
    }
}