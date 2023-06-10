using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetra4bica.Util.StructIterators {

    /// <summary>
    /// Struct enumerator of bunch of <see cref="Vector2Int"/> that allows to set offset for this bunch.
    /// </summary>
    public struct HashSetVector2IntWrapper : IEnumerator<Vector2Int>, IEnumerator, IDisposable {

        readonly HashSet<Vector2Int> cells;

        HashSet<Vector2Int>.Enumerator cellsEnumerator;

        Vector2Int shift;

        public HashSetVector2IntWrapper(HashSet<Vector2Int> cells, Vector2Int shift) {
            this.cells = cells;
            this.cellsEnumerator = cells.GetEnumerator();
            this.shift = shift;
        }

        public HashSetVector2IntWrapper Shift(Vector2Int additionalShift)
            => new(cells, shift + additionalShift);

        public Vector2Int Current => cellsEnumerator.Current + shift;


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        public bool MoveNext() => cellsEnumerator.MoveNext();

        void IEnumerator.Reset() => cellsEnumerator = cells.GetEnumerator();

        public void Dispose() { }
    }
}