using UnityEngine;

namespace Tetra4bica.Core
{

    public interface IVisualSettings
    {
        public float CellSize { get; }
        public Transform BricksParent { get; }
        public float ScoreParticlesFlightTimeMin { get; }
        public float ScoreParticlesFlightTimeMax { get; }

        public Vector3 BottomLeftPoint { get; }
    }
}
