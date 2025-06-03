using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.Sound
{

    public class ColorTableSfx : MonoBehaviour
    {

        [Inject]
        private IGameEvents _gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        private AudioSource _audioSource;

        [SerializeField, FormerlySerializedAs("wallDestructionSfx")]
        private AudioResource _wallDestructionSfx;


        private void Awake()
        {
            _gameLogic.EliminatedBricksStream.Subscribe(
                _ => SoundUtils.PlaySound(_audioSource, _wallDestructionSfx)
            );
        }
    }
}
