using UnityEngine;

namespace Tetra4bica.Core
{

    /// <summary> Mutable struct keeping coordinates and velocity of a projectile. </summary>
    public struct Projectile
    {

        public bool IsActive;
        public Vector2 Position;
        public Vector2Int Direction;

        public Projectile(Vector2 position, Vector2Int direction)
        {
            IsActive = true;
            Position = position;
            Direction = direction;
        }
    }
}
