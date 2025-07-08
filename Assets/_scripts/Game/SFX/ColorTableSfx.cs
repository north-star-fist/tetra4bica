using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Sound
{

    public class ColorTableSfx : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("wallDestructionSfx")]
        private AudioResource _wallDestructionSfx;

        [Inject]
        private IGameEvents _gameLogic;
        [Inject]
        private IAudioSourceManager _audioManager;

        private AudioSource _audioSource;


        private void Start()
        {
            _gameLogic.EliminatedBricksStream.Subscribe(
                _ => SoundUtils.PlaySound(_audioSource, _wallDestructionSfx)
            );
            _audioSource = _audioManager.GetAudioSource(AudioSourceId.SoundEffects);
        }
    }
}
