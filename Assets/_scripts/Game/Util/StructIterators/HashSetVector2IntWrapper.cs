using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetra4bica.Util.StructIterators
{

    /// <summary>
    /// Struct enumerator of bunch of <see cref="Vector2Int"/> that allows to set offset for this bunch.
    /// </summary>
    public struct HashSetVector2IntWrapper : IEnumerator<Vector2Int>
    {

        readonly HashSet<Vector2Int> _cells;

        HashSet<Vector2Int>.Enumerator _cellsEnumerator;

        Vector2Int _shift;

        public HashSetVector2IntWrapper(HashSet<Vector2Int> cells, Vector2Int shift)
        {
            _cells = cells;
            _cellsEnumerator = cells.GetEnumerator();
            _shift = shift;
        }

        public HashSetVector2IntWrapper Shift(Vector2Int additionalShift)
            => new(_cells, _shift + additionalShift);

        public Vector2Int Current => _cellsEnumerator.Current + _shift;


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        public bool MoveNext() => _cellsEnumerator.MoveNext();

        void IEnumerator.Reset() => _cellsEnumerator = _cells.GetEnumerator();

        public void Dispose() { }
    }
}
