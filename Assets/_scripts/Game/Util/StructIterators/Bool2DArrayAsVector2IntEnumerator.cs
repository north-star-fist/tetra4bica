using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Util.StructIterators
{
    public struct Bool2DArrayAsVector2IntEnumerator : IEnumerator<Vector2Int>
    {

        readonly bool[,] array2D;

        readonly Vector2Int size;

        int currentX; int currentY;

        Vector2Int next;

        bool noNext;

        bool movedNextInitially;

        public Bool2DArrayAsVector2IntEnumerator(bool[,] array2D)
        {
            this.array2D = array2D;
            size = array2D != null ? v2i(array2D.GetLength(0), array2D.GetLength(1)) : Vector2Int.zero;
            currentX = 0;
            currentY = 0;
            next = default;
            noNext = false;
            movedNextInitially = false;
        }


        public Vector2Int Current
        {
            get
            {
                return !movedNextInitially || noNext
                    ? throw new InvalidOperationException("No more elements to iterate!")
                    : next;
            }
        }

        public bool MoveNext()
        {
            noNext = !getNext(out next);
            movedNextInitially = true;
            if (!noNext)
            {
                increaseIndices();
            }
            return !noNext;
        }

        private bool getNext(out Vector2Int next)
        {
            next = default;
            if (size == Vector2Int.zero || noNext || currentX >= size.x)
            {
                return false;
            }
            bool arrayElement = array2D[currentX, currentY];
            while (!arrayElement)
            {
                if (!increaseIndices())
                {
                    return false;
                }
                arrayElement = array2D[currentX, currentY];
            };
            next = v2i(currentX, currentY);
            return true;
        }

        private bool increaseIndices()
        {
            if (currentX >= size.x)
            {
                return false;
            }
            currentY++;
            if (currentY >= size.y)
            {
                currentX++;
                currentY = 0;
            }
            return currentX < size.x;
        }


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        void IEnumerator.Reset()
        {
            currentX = 0;
            currentY = 0;
            next = default;
            noNext = false;
            movedNextInitially = false;
        }

        public void Dispose() { }
    }

    public struct Bool2DArrayAsVector2IntEnumerable : IEnumerable<Vector2Int>
    {

        bool[,] array2D;

        public Bool2DArrayAsVector2IntEnumerable(bool[,] array2D)
        {
            this.array2D = array2D;
        }


        /// <summary> Public non allocating enumerator. </summary>
        public Bool2DArrayAsVector2IntEnumerator GetEnumerator()
        {
            return new Bool2DArrayAsVector2IntEnumerator(array2D);
        }

        // private methods for IEnumerable inheritance
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
    }

    static public class Bool2DArrayAsVector2IntEnumerableExtensions
    {
        public static Bool2DArrayAsVector2IntEnumerable AsEnumerable(this bool[,] array2D)
        {
            return new Bool2DArrayAsVector2IntEnumerable(array2D);
        }
    }
}
