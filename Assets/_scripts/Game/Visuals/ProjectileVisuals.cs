using System;
using System.Threading.Tasks;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Graphics
{

    [RequireComponent(typeof(ParticleSystem))]
    public class ProjectileVisuals : MonoBehaviour
    {

        private const int PARTICLES_ARRAY_START_SIZE = 8;

        [Inject]
        private IGameEvents _gameEvents;

        private ParticleSystem _pSystem;
        private ParticleSystem.Particle[] _particles;

        private int _projectilesCount = 0;

        private void Awake()
        {
            _pSystem = GetComponent<ParticleSystem>();
            _gameEvents.ProjectileCoordinatesStream.Subscribe(renderProjectile);
            _gameEvents.FrameUpdateStream.Subscribe(frameRefresh);
            _particles = new ParticleSystem.Particle[PARTICLES_ARRAY_START_SIZE];
        }

        private void frameRefresh(float _)
        {
            drawParticles(_projectilesCount);
            _projectilesCount = 0;
        }

        private async Task drawParticles(int projectilesCount)
            => _pSystem.SetParticles(_particles, projectilesCount);

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
