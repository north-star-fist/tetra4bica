using System;
using System.Collections;
using System.Collections.Generic;
using Tetra4bica.Util;
using Tetra4bica.Util.StructIterators;
using UnityEngine;

namespace Tetra4bica.Core {

    /// <summary>
    /// Class that helps to work with player's tetramino.
    /// It contains the actual tetramino with it's position and orientation on the 2D map.
    /// </summary>
    public struct PlayerTetromino : IEquatable<PlayerTetromino>, IEnumerable<Vector2Int> {
        // Player tetraminos. Default and rotated variants
        // Dir => Matrix
        readonly IDictionary<Vector2Int, CellFragment> dirFormMap;

        public Vector2Int Position;
        // Pivot to rotate around (as shift from the bottom left corner of the form)
        Vector2Int pivot;
        public Vector2Int muzzle;
        public Vector2Int direction;
        public readonly CellColor Color;

        public Vector2Int Size {
            get {
                return dirFormMap[direction].Size;
            }
        }

        public PlayerTetromino(Vector2Int position, CellColor playerColor) : this(
            position: position,
            formMatrix: new bool[,] {
                {true, true, true},
                {false, true, false}
            },
            pivot: new Vector2Int(0, 1),
            muzzle: new Vector2Int(2, 1),
            direction: Vector2Int.right,
            playerColor: playerColor
        ) {

        }

        public PlayerTetromino(
            Vector2Int position,
            bool[,] formMatrix,
            Vector2Int pivot,
            Vector2Int muzzle,
            Vector2Int direction,
            CellColor playerColor
        ) : this(
            position,
            CellFragment.Fragment(formMatrix, out var _),
            pivot,
            muzzle,
            direction,
            playerColor
        ) { }

        public PlayerTetromino(
            Vector2Int position,
            CellFragment formMatrix,
            Vector2Int pivot,
            Vector2Int muzzle,
            Vector2Int direction,
            CellColor playerColor
        ) {
            this.Position = position;
            this.pivot = pivot;
            this.muzzle = muzzle;
            this.direction = direction;
            this.Color = playerColor;

            Dictionary<Vector2Int, CellFragment> forms = new Dictionary<Vector2Int, CellFragment> {
                { direction, formMatrix }
            };

            CellFragment curForm = formMatrix;
            Vector2Int curDir = direction;
            for (int i = 0; i < 3; i++) {
                curForm = CellFragment.Fragment(MatrixUtil.RotateBy90(curForm, true), out var _);
                curDir = Direction.RotateBy90(curDir, true);
                forms.Add(curDir, curForm);
            }
            dirFormMap = forms;
        }

        PlayerTetromino(
            Vector2Int position,
            IDictionary<Vector2Int, CellFragment> matrices,
            Vector2Int pivot,
            Vector2Int muzzle,
            Vector2Int direction,
            CellColor playerColor
        ) {
            this.muzzle = muzzle;
            this.direction = direction;
            this.Position = position;
            this.pivot = pivot;
            this.Color = playerColor;
            this.dirFormMap = matrices;
        }

        public PlayerTetromino Rotate(bool clockwise) {
            Vector2Int newDir = Direction.RotateBy90(direction, clockwise);
            CellFragment resForm = dirFormMap[newDir];

            Vector2Int newBounds = resForm.Size;
            Vector2Int newPivot = rotateAndShift(pivot, clockwise, newBounds);
            var newPosition = Position + (pivot - newPivot);
            return new PlayerTetromino(
                newPosition,
                dirFormMap,
                newPivot,
                rotateAndShift(muzzle, clockwise, newBounds),
                newDir,
                Color);
        }

        Vector2Int rotateAndShift(Vector2Int toRotate, bool clockwise, Vector2Int newBounds) {
            return shift(newBounds, MatrixUtil.RotateBy90(toRotate, clockwise), clockwise);
        }

        static Vector2Int shift(Vector2Int newBounds, Vector2Int rotatedVector, bool clockwise) {
            return new Vector2Int(
                clockwise ? rotatedVector.x : newBounds.x - 1 + rotatedVector.x,
                !clockwise ? rotatedVector.y : newBounds.y - 1 + rotatedVector.y
            );
        }

        public PlayerTetromino withPosition(Vector2Int playerCoordinates) =>
            new PlayerTetromino(playerCoordinates, dirFormMap, pivot, muzzle, direction, Color);

        public CellFragment.VerticalCellsEnumerable GetVerticalCells(int wallX) {
            var curForm = dirFormMap[direction];
            return curForm.GetVerticalCells(wallX - Position.x, Position);
        }

        /// <summary> Public non GC enumerator of cell positions. </summary>
        public HashSetVector2IntWrapper GetEnumerator() {
            var curForm = dirFormMap[direction];
            return curForm.GetEnumerator(Position);
        }

        // private methods for IEnumerable inheritance
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        public bool Contains(Vector2Int cell) {
            return dirFormMap[direction].Contains(cell - Position);
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }
            if (obj is not PlayerTetromino) {
                return false;
            }
            return Equals((PlayerTetromino)obj);
        }
        public bool Equals(PlayerTetromino other) =>
            Position.Equals(other.Position)
            && pivot.Equals(other.pivot)
            && muzzle.Equals(other.muzzle)
            && direction.Equals(other.direction)
            && Color == other.Color
            && dirFormMap[direction].Equals(other.dirFormMap[direction]);

        public override int GetHashCode() => HashCode.Combine(dirFormMap, Position, pivot, muzzle, direction, Color);

        public static bool operator ==(PlayerTetromino left, PlayerTetromino right)
            => EqualityComparer<PlayerTetromino>.Default.Equals(left, right);
        public static bool operator !=(PlayerTetromino left, PlayerTetromino right) => !(left == right);
    }
}