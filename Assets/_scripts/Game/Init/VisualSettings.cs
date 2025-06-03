using System;
using UnityEngine;

namespace Tetra4bica.Init
{

    [Serializable]
    public class VisualSettings
    {
        [SerializeField]
        private float _cellSize = 1;

        [SerializeField]
        private Transform _bottomLeftTunnelPoint;
        [SerializeField]
        private Transform _bricksParent;
        [
            SerializeField,
            Tooltip("When cell is eliminated score particle flies from the cell's location to the score lable. " +
                "This is the flight time")
        ]
        private float _scoreParticlesFlightTimeMin = .3f;
        [
            SerializeField,
            Tooltip("When cell is eliminated score particle flies from the cell's location " +
                "to the score lable. This is the flight time")
        ]
        private float _scoreParticlesFlightTimeMax = 0.7f;

        public float CellSize => _cellSize;
        public Transform BricksParent => _bricksParent;
        public float ScoreParticlesFlightTimeMin => _scoreParticlesFlightTimeMin;
        public float ScoreParticlesFlightTimeMax => _scoreParticlesFlightTimeMax;

        Vector3? _bottomLeftTunnelPosition = null;

        public Vector3 BottomLeftPoint
        {
            get
            {
                if (_bottomLeftTunnelPosition.HasValue)
                {
                    return _bottomLeftTunnelPosition.Value;
                }
                else
                {
                    return _bottomLeftTunnelPoint != null
                        ? _bottomLeftTunnelPoint.position
                        : Vector3.zero;
                }
            }
        }

        VisualSettings()
        {
        }

        public VisualSettings(float cellSize, Vector3 bottomLeftTunnelPoint, Transform bricksParent = null)
        {
            this._cellSize = cellSize;
            this._bottomLeftTunnelPosition = bottomLeftTunnelPoint;
            this._bricksParent = bricksParent;
        }
    }
}
