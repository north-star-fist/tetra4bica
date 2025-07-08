using System;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;


namespace Tetra4bica.Sound
{

    public class GameMainEventsSfx : MonoBehaviour, IMainGameEventsSfx
    {
        [SerializeField, FormerlySerializedAs("gameStartSfx")]
        private AudioResource _gameStartSfx;
        [SerializeField, FormerlySerializedAs("gameOverSfx")]
        private AudioResource _gameOverSfx;
        [SerializeField, FormerlySerializedAs("gameplayBgmAudioSource")]
        private AudioSource _gameplayBgmAudioSource;
        [SerializeField, FormerlySerializedAs("gameOverBgmAudioSource")]
        private AudioSource _gameOverBgmAudioSource;


        private IAudioSourceManager _audioManager;

        private AudioSource _sfxAudioSource;


        public void Setup(
            IObservable<Unit> gameStartedStream,
            IObservable<Unit> gameOverStream,
            IAudioSourceManager audioManager
        )
        {
            gameStartedStream.Subscribe(_ => startBgmAfterSfx(_gameStartSfx, _gameplayBgmAudioSource));
            gameOverStream.Subscribe(_ => startBgmAfterSfx(_gameOverSfx, _gameOverBgmAudioSource));

            _sfxAudioSource = audioManager.GetAudioSource(AudioSourceId.SoundEffects);
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
