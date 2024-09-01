using System;
using UnityEngine;

namespace Tetra4bica.Init
{

    [Serializable]
    public class VisualSettings
    {
        [SerializeField]
        private float cellSize = 1;

        [SerializeField]
        private Transform bottomLeftTunnelPoint;
        [SerializeField]
        private Transform bricksParent;
        [
            SerializeField,
            Tooltip("When cell is eliminated score particle flies from the cell's location to the score lable. " +
                "This is the flight time")
        ]
        private float scoreParticlesFlightTimeMin = .3f;
        [
            SerializeField,
            Tooltip("When cell is eliminated score particle flies from the cell's location " +
                "to the score lable. This is the flight time")
        ]
        private float scoreParticlesFlightTimeMax = 0.7f;

        public float CellSize => cellSize;
        public Transform BricksParent => bricksParent;
        public float ScoreParticlesFlightTimeMin => scoreParticlesFlightTimeMin;
        public float ScoreParticlesFlightTimeMax => scoreParticlesFlightTimeMax;

        Vector3? bottomLeftTunnelPosition = null;

        public Vector3 BottomLeftPoint
        {
            get
            {
                if (bottomLeftTunnelPosition.HasValue)
                {
                    return bottomLeftTunnelPosition.Value;
                }
                else
                {
                    return bottomLeftTunnelPoint != null
                        ? bottomLeftTunnelPoint.position
                        : Vector3.zero;
                }
            }
        }

        VisualSettings()
        {
        }

        public VisualSettings(float cellSize, Vector3 bottomLeftTunnelPoint, Transform bricksParent = null)
        {
            this.cellSize = cellSize;
            this.bottomLeftTunnelPosition = bottomLeftTunnelPoint;
            this.bricksParent = bricksParent;
        }
    }
}
