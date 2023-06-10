using Sergei.Safonov.Audio;
using System;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Sound {

    public class ProjectileSfx : MonoBehaviour {

        [Inject]
        IGameEvents gameLogic;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource audioSource;

        public AudioResource particleFrozenSfx;

        private void Awake() {
            Setup(gameLogic.FrozenProjectilesStream);
        }


        void Setup(IObservable<Vector2Int> projectileFrozenStream) {
            projectileFrozenStream.Subscribe(_ => SoundUtils.PlaySound(audioSource, particleFrozenSfx));
        }
    }
}