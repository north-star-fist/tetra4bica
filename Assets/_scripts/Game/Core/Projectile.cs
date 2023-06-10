using UnityEngine;

namespace Tetra4bica.Core {

    /// <summary> Mutable struct keeping coordinates and velocity of a projectile. </summary>
    public struct Projectile {

        public bool active;
        public Vector2 position;
        public Vector2Int direction;

        public Projectile(Vector2 position, Vector2Int direction) {
            this.active = true;
            this.position = position;
            this.direction = direction;
        }
    }
}