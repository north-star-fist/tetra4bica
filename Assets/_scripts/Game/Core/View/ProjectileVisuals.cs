using System;
using System.Threading.Tasks;
using Sergei.Safonov.Audio;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Graphics
{

    [RequireComponent(typeof(ParticleSystem))]
    public class ProjectileVisuals : MonoBehaviour
    {

        private const int PARTICLES_ARRAY_START_SIZE = 8;


        private ParticleSystem _pSystem;
        private ParticleSystem.Particle[] _particles;

        private int _projectilesCount = 0;


        [SerializeField, FormerlySerializedAs("particleFrozenSfx")]
        private Sergei.Safonov.Audio.AudioResource _particleFrozenSfx;

        IGameEvents _gameLogic;

        private IAudioSourceManager _audioManager;

        AudioSource _audioSource;


        private CompositeDisposable _disposables = new CompositeDisposable();


        private void Awake()
        {
            _pSystem = GetComponent<ParticleSystem>();
            _particles = new ParticleSystem.Particle[PARTICLES_ARRAY_START_SIZE];
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _disposables = null;
        }

        public void Setup(
            IGameEvents gameEvents,
            IObservable<Vector2Int> projectileFrozenStream,
            IAudioSourceManager audioManager
        )
        {
            _disposables.Dispose();
            _disposables = new CompositeDisposable();

            gameEvents.ProjectileCoordinatesStream.Subscribe(renderProjectile).AddTo(_disposables);
            gameEvents.FrameUpdateStream.Subscribe(frameRefresh).AddTo(_disposables);

            _audioSource = audioManager.GetAudioSource(AudioSourceId.SoundEffects);
            projectileFrozenStream.Subscribe(_ => SoundUtils.PlaySound(_audioSource, _particleFrozenSfx));
        }


        private void frameRefresh(float _)
        {
            _pSystem.SetParticles(_particles, _projectilesCount);
            _projectilesCount = 0;
        }

        void renderProjectile(Vector2 projectile)
        {
            enlargeParticlesArrayIfNeeded(_projectilesCount);
            _particles[_projectilesCount] = new ParticleSystem.Particle();
            // TODO scale position according map scale.
            _particles[_projectilesCount].position = projectile;
            _particles[_projectilesCount].startSize = 1;
            _projectilesCount++;

            void enlargeParticlesArrayIfNeeded(int projectileCount)
            {
                if (_particles.Length <= projectileCount)
                {
                    ParticleSystem.Particle[] largerCapacityArray = new ParticleSystem.Particle[_particles.Length * 2];
                    Array.Copy(_particles, largerCapacityArray, _particles.Length);
                    _particles = largerCapacityArray;
                }
            }
        }
    }
}
