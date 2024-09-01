using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound
{

    public class ColorTableSfx : MonoBehaviour
    {

        [Inject]
        IGameEvents gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource audioSource;

        [SerializeField]
        private AudioResource wallDestructionSfx;


        private void Awake()
        {
            gameLogic.EliminatedBricksStream.Subscribe(
                _ => SoundUtils.PlaySound(audioSource, wallDestructionSfx)
            );
        }
    }
}
