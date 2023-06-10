using System;
using UnityEngine;

namespace Tetra4bica.Init {

    [Serializable]
    public class VisualSettings {

        public float cellSize = 1;

        [SerializeField]
        Transform bottomLeftTunnelPoint;

        public Transform bricksParent;
        [Tooltip("When cell is eliminated score particle flies from the cell's location to the score lable. This is the flight time")]
        public float scoreParticlesFlightTimeMin = .3f;
        [Tooltip("When cell is eliminated score particle flies from the cell's location to the score lable. This is the flight time")]
        public float scoreParticlesFlightTimeMax = 0.7f;

        // TODO: Rewrite with Option<> later.
        bool vectorValue = false;
        Vector3 _bottomLeftTunnelPoint = Vector3.negativeInfinity;

        public Vector3 BottomLeftPoint {
            get {
                if (vectorValue) {
                    return _bottomLeftTunnelPoint;
                } else {
                    return bottomLeftTunnelPoint != null
                        ? bottomLeftTunnelPoint.position
                        : Vector3.zero;
                }
            }
        }

        VisualSettings() {
        }

        public VisualSettings(float cellSize, Vector3 bottomLeftTunnelPoint, Transform bricksParent = null) {
            this.cellSize = cellSize;
            vectorValue = true;
            this._bottomLeftTunnelPoint = bottomLeftTunnelPoint;
            this.bricksParent = bricksParent;
        }
    }
}