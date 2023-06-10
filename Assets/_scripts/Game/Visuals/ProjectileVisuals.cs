using System;
using System.Threading.Tasks;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Graphics {

    [RequireComponent(typeof(ParticleSystem))]
    public class ProjectileVisuals : MonoBehaviour {

        private const int PARTICLES_ARRAY_START_SIZE = 8;

        [Inject]
        IGameEvents gameEvents;

        ParticleSystem _particleSystem;
        ParticleSystem.Particle[] particles;

        int projectilesCount = 0;

        private void Awake() {
            _particleSystem = GetComponent<ParticleSystem>();
            gameEvents.ProjectileCoordinatesStream.Subscribe(renderProjectile);
            gameEvents.FrameUpdateStream.Subscribe(frameRefresh);
            particles = new ParticleSystem.Particle[PARTICLES_ARRAY_START_SIZE];
        }

        private void frameRefresh(float _) {
            drawParticles(projectilesCount);
            projectilesCount = 0;
        }

        private async Task drawParticles(int projectilesCount)
            => _particleSystem.SetParticles(particles, projectilesCount);

        void renderProjectile(Vector2 projectile) {
            enlargeParticlesArrayIfNeeded(projectilesCount);
            particles[projectilesCount] = new ParticleSystem.Particle();
            // TODO scale position according map scale.
            particles[projectilesCount].position = projectile;
            particles[projectilesCount].startSize = 1;
            projectilesCount++;

            void enlargeParticlesArrayIfNeeded(int projectileCount) {
                if (particles.Length <= projectileCount) {
                    ParticleSystem.Particle[] largerCapacityArray = new ParticleSystem.Particle[particles.Length * 2];
                    Array.Copy(particles, largerCapacityArray, particles.Length);
                    particles = largerCapacityArray;
                }
            }
        }
    }
}
