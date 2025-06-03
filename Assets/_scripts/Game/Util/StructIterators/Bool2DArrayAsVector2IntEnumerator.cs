using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Util.StructIterators
{
    public struct Bool2DArrayAsVector2IntEnumerator : IEnumerator<Vector2Int>
    {

        private readonly bool[,] _array2D;

        private readonly Vector2Int _size;

        private int _currentX;
        private int _currentY;

        private Vector2Int _next;

        private bool _noNext;

        private bool _movedNextInitially;

        public Bool2DArrayAsVector2IntEnumerator(bool[,] array2D)
        {
            _array2D = array2D;
            _size = array2D != null ? v2i(array2D.GetLength(0), array2D.GetLength(1)) : Vector2Int.zero;
            _currentX = 0;
            _currentY = 0;
            _next = default;
            _noNext = false;
            _movedNextInitially = false;
        }


        public Vector2Int Current
        {
            get
            {
                return !_movedNextInitially || _noNext
                    ? throw new InvalidOperationException("No more elements to iterate!")
                    : _next;
            }
        }

        public bool MoveNext()
        {
            _noNext = !getNext(out _next);
            _movedNextInitially = true;
            if (!_noNext)
            {
                increaseIndices();
            }
            return !_noNext;
        }

        private bool getNext(out Vector2Int next)
        {
            next = default;
            if (_size == Vector2Int.zero || _noNext || _currentX >= _size.x)
            {
                return false;
            }
            bool arrayElement = _array2D[_currentX, _currentY];
            while (!arrayElement)
            {
                if (!increaseIndices())
                {
                    return false;
                }
                arrayElement = _array2D[_currentX, _currentY];
            };
            next = v2i(_currentX, _currentY);
            return true;
        }

        private bool increaseIndices()
        {
            if (_currentX >= _size.x)
            {
                return false;
            }
            _currentY++;
            if (_currentY >= _size.y)
            {
                _currentX++;
                _currentY = 0;
            }
            return _currentX < _size.x;
        }


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        void IEnumerator.Reset()
        {
            _currentX = 0;
            _currentY = 0;
            _next = default;
            _noNext = false;
            _movedNextInitially = false;
        }

        public void Dispose() { }
    }

    public struct Bool2DArrayAsVector2IntEnumerable : IEnumerable<Vector2Int>
    {

        private readonly bool[,] _array2D;

        public Bool2DArrayAsVector2IntEnumerable(bool[,] array2D)
        {
            this._array2D = array2D;
        }


        /// <summary> Public non allocating enumerator. </summary>
        public Bool2DArrayAsVector2IntEnumerator GetEnumerator()
        {
            return new Bool2DArrayAsVector2IntEnumerator(_array2D);
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
