using System;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound
{

    public class GameMainEventsSfx : MonoBehaviour
    {

        [Inject]
        private IGameEvents _gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        private AudioSource _sfxAudioSource;

        [SerializeField]
        private AudioResource _gameStartSfx;
        [SerializeField]
        private AudioResource _gameOverSfx;
        [SerializeField]
        private AudioSource _gameplayBgmAudioSource;
        [SerializeField]
        private AudioSource _gameOverBgmAudioSource;

        private void Awake()
        {
            Setup(
                _gameLogic.GamePhaseStream.Scan
                (
                    // Getting switching to Started phase only after NotStarted or GameOver phases
                    // keeping in mind that the Game can not be started at Paused state (if it can -
                    // game starting sound is played)
                    (GamePhase.NotStarted, GamePhase.NotStarted),
                    (phaseSwitch, newPhase) =>
                    {
                        return (phaseSwitch.Item2, newPhase);
                    }
                ).Where(phaseSwitch => phaseSwitch.Item1 is GamePhase.GameOver or GamePhase.NotStarted
                    && phaseSwitch.Item2 is GamePhase.Started).Select(_ => Unit.Default),
                _gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            );
        }

        void Setup(IObservable<Unit> gameStartedStream, IObservable<Unit> gameOverStream)
        {
            gameStartedStream.Subscribe(_ => startBgmAfterSfx(_gameStartSfx, _gameplayBgmAudioSource));
            gameOverStream.Subscribe(_ => startBgmAfterSfx(_gameOverSfx, _gameOverBgmAudioSource));
        }

        private void startBgmAfterSfx(AudioResource sfx, AudioSource bgmAudioSource)
        {
            _gameOverBgmAudioSource.Stop();
            _gameplayBgmAudioSource.Stop();
            SoundUtils.PlaySound(_sfxAudioSource, sfx);
            double bgmDelaySeconds = _sfxAudioSource.clip.length;
            bgmAudioSource.PlayScheduled(AudioSettings.dspTime + bgmDelaySeconds);
        }
    }
}
