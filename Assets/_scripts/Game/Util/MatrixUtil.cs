using Sergei.Safonov.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Util {
    public static class MatrixUtil {

        static readonly Quaternion clockwiseTurn = Quaternion.Euler(0, 0, -90);
        static readonly Quaternion counterClockwiseTurn = Quaternion.Euler(0, 0, 90);

        /// <summary> Rotates 2D array by 90 degres clock or counter clockwise. </summary>
        static public T[,] RotateBy90<T>(T[,] srcMatrix, bool clockwise) {
            int srcWidth = srcMatrix.GetLength(0);
            int srcHeight = srcMatrix.GetLength(1);

            var newBounds = v2i(srcHeight, srcWidth);
            T[,] result = new T[newBounds.x, newBounds.y];
            for (int x = 0; x < srcWidth; x++) {
                for (int y = 0; y < srcHeight; y++) {
                    var rotatedPosition = RotateBy90(v2i(x, y), clockwise);
                    var newPosition = shift(newBounds, rotatedPosition, clockwise);
                    result[newPosition.x, newPosition.y] = srcMatrix[x, y];
                }
            }
            return result;
        }

        static public T[,] RotateBy180<T>(T[,] srcMatrix) {
            // Non effective, allocates medium array
            // TODO: rewrite
            return RotateBy90(RotateBy90(srcMatrix, true), true);
        }

        static public Vector2Int RotateBy90(Vector2Int vector, bool clockwise) {
            var vector3 = vector.toVector3();
            var turnQuaternion = clockwise ? clockwiseTurn : counterClockwiseTurn;
            return (turnQuaternion * vector3).toVector2Int();
        }

        /// <summary>
        /// Rotates group of Vectors by 90 degres clock or counter clockwise shifting them by some
        /// delta making them all positive (both coordinates) and the minimal possible rotated vector becomes [0, 0] (or positive).
        /// <see cref="RotateBy90{T}(T[,], bool)"/>
        /// </summary>
        static public IEnumerable<Vector2Int> RotateBy90(IEnumerable<Vector2Int> srcMatrix, bool clockwise) {
            Vector2Int minPoint = v2i(int.MaxValue, int.MaxValue);
            Vector2Int maxPoint = v2i(int.MinValue, int.MinValue);
            foreach (var point in srcMatrix) {
                minPoint = v2i(Math.Min(minPoint.x, point.y), Math.Min(minPoint.y, point.x));
                maxPoint = v2i(Math.Max(maxPoint.x, point.y), Math.Max(maxPoint.y, point.x));
            }

            var newBounds = maxPoint - minPoint + Vector2Int.one;

            LinkedList<Vector2Int> result = new LinkedList<Vector2Int>();
            foreach (Vector2Int point in srcMatrix) {
                var rotatedPosition = RotateBy90(point, clockwise);
                var newPosition = shift(newBounds, rotatedPosition, clockwise);
                result.AddLast(newPosition);
            }
            return result;
        }

        // Shifts rotated vector to handle negative indices
        static Vector2Int shift(Vector2Int newBounds, Vector2Int rotatedPos, bool clockwise) {
            return clockwise
                ? v2i(rotatedPos.x, newBounds.y - 1 + rotatedPos.y)
                : v2i(newBounds.x - 1 + rotatedPos.x, rotatedPos.y);
        }
    }
}