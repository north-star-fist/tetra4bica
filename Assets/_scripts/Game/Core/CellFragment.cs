using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tetra4bica.Util.StructIterators;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Core
{

    /// <summary> Type representing a bunch of cells. </summary>
    // Immutable.
    public struct CellFragment : IEquatable<CellFragment>, IEnumerable<Vector2Int>
    {

        public static readonly CellFragment SINGLE_CELL =
            new(new HashSet<Vector2Int>() { Vector2Int.zero }, Vector2Int.one);

        private const int INITIAL_CELL_SET_CAPACITY = 16;

        static readonly HashSetVector2IntWrapper EMPTY_FRAGMENT_ENUMERATOR
            = new(new HashSet<Vector2Int>(), Vector2Int.zero);

        public readonly Vector2Int Size;

        private readonly HashSet<Vector2Int> _cellsLocal;

        private int _cellsSetHash;
        private bool _isCellsSetHashCached;

        // Shift to the first non-zero cell. It is kept not to run through all cells twice while object instantiating.
        private Vector2Int _cellsShift;


        public static CellFragment Fragment(IEnumerable<Vector2Int> enumerable, out Vector2Int minPointShift)
        {
            var cells = createCellSet(enumerable, out minPointShift, out var maxPointShift);
            return createFragment(cells, minPointShift, maxPointShift);
        }

        public static CellFragment Fragment(Bool2DArrayAsVector2IntEnumerable enumerable, out Vector2Int minPointShift)
        {
            var cells = createCellSet(enumerable, out minPointShift, out var maxPointShift);
            return createFragment(cells, minPointShift, maxPointShift);
        }

        public static CellFragment Fragment(bool[,] fragmentCells, out Vector2Int minPointShift)
            => Fragment(fragmentCells.AsEnumerable(), out minPointShift);

        CellFragment(HashSet<Vector2Int> cells, Vector2Int size, Vector2Int cellsShift = default)
        {
            _cellsLocal = (cells == null) || cells.Count == 0 ? null : cells;
            Size = size;
            _cellsSetHash = 0;
            _isCellsSetHashCached = false;
            _cellsShift = cellsShift;
        }

        private static void paintConnectedCells(
            CellFragment cellsSource,
            Vector2Int excludePoint,
            HashSet<Vector2Int> canvas,
            Vector2Int startingPoint,
            ref Vector2Int minPos,
            ref Vector2Int maxPos
        )
        {
            // Using Flood fill algorithm
            if (startingPoint == excludePoint)
            {
                return;
            }

            // 1.If node is not Inside return.
            if (!cellsSource.Contains(startingPoint))
            {
                return;
            }

            // 2. Painting
            if (canvas.Contains(startingPoint))
            {
                // already painted
                return;
            }
            canvas.Add(startingPoint);
            minPos = Vector2Int.Min(minPos, startingPoint);
            maxPos = Vector2Int.Max(maxPos, startingPoint);

            // 3.Perform Flood - fill one step to the south of node.
            // 4.Perform Flood - fill one step to the north of node
            // 5.Perform Flood - fill one step to the west of node
            // 6.Perform Flood - fill one step to the east of node
            foreach (var dir in Direction.FOUR_DIRECTIONS)
            {
                paintConnectedCells(cellsSource, excludePoint, canvas, startingPoint + dir, ref minPos, ref maxPos);
            }

            // 7.Return.
        }

        public uint Count() => (uint)(_cellsLocal != null ? _cellsLocal.Count : 0);

        public bool IsEmpty() => Size == Vector2Int.zero;

        public bool Contains(Vector2Int cell) => _cellsLocal != null && _cellsLocal.Contains(cell - _cellsShift);


        /// <summary> Returns coordinates of occupied (colored) cells for specified x coordinate. </summary>
        public VerticalCellsEnumerable GetVerticalCells(int x, Vector2Int additionalShift) => new(this, x, additionalShift);

        /// <summary> Public non GC enumerator of cell positions. </summary>
        public HashSetVector2IntWrapper GetEnumerator() => GetEnumerator(Vector2Int.zero);

        // private methods for IEnumerable inheritance
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        public HashSetVector2IntWrapper GetEnumerator(Vector2Int shift)
            => _cellsLocal == null ? EMPTY_FRAGMENT_ENUMERATOR : new HashSetVector2IntWrapper(_cellsLocal, _cellsShift + shift);

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is not CellFragment)
            {
                return false;
            }
            return Equals((CellFragment)obj);
        }

        public bool Equals(CellFragment other)
        {
            if (other._cellsLocal == null)
            {
                return _cellsLocal == null;
            }
            if (Size != other.Size)
            {   // Rect bounds
                return false;
            }
            if (Count() != other.Count())
            { // Non empty cells count
                return false;
            }
            foreach (var cell in _cellsLocal)
            {
                if (!other._cellsLocal.Contains(cell + _cellsShift - other._cellsShift))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            if (_isCellsSetHashCached)
            {
                _cellsSetHash = calculateCellsHash();
                _isCellsSetHashCached = true;
            }
            return _cellsSetHash;
        }

        int calculateCellsHash()
        {
            int hash = 0;
            foreach (var cell in _cellsLocal)
            {
                hash += hash * -1521134295 + (cell + _cellsShift).GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(CellFragment left, CellFragment right) => EqualityComparer<CellFragment>.Default.Equals(left, right);
        public static bool operator !=(CellFragment left, CellFragment right) => !(left == right);

        private static CellFragment createFragment(HashSet<Vector2Int> cells, Vector2Int minPointShift, Vector2Int maxPointShift)
        {
            Vector2Int size = maxPointShift - minPointShift + Vector2Int.one;
            if (size == Vector2Int.zero)
            {
                return default;
            }
            else
            {
                // TODO: cache regions
                return new CellFragment(cells, size, -minPointShift);
            }
        }

        static private HashSet<Vector2Int> createCellSet(
            IEnumerable<Vector2Int> cells,
            out Vector2Int minPoint,
            out Vector2Int maxPoint
        )
        {
            minPoint = v2i(int.MaxValue, int.MaxValue);
            maxPoint = v2i(int.MinValue, int.MinValue);
            HashSet<Vector2Int> hashSet = new(INITIAL_CELL_SET_CAPACITY);
            foreach (var cell in cells)
            {
                addCell(hashSet, cell, ref minPoint, ref maxPoint);
            }
            return hashSet;
        }

        static private HashSet<Vector2Int> createCellSet(
            Bool2DArrayAsVector2IntEnumerable cells,
            out Vector2Int minPoint,
            out Vector2Int maxPoint
        )
        {
            minPoint = v2i(int.MaxValue, int.MaxValue);
            maxPoint = v2i(int.MinValue, int.MinValue);
            HashSet<Vector2Int> hashSet = new(INITIAL_CELL_SET_CAPACITY);
            foreach (var cell in cells)
            {
                addCell(hashSet, cell, ref minPoint, ref maxPoint);
            }
            return hashSet;
        }

        private static void addCell(HashSet<Vector2Int> hashSet, Vector2Int cell, ref Vector2Int minPoint, ref Vector2Int maxPoint)
        {
            minPoint = v2i(Math.Min(minPoint.x, cell.x), Math.Min(minPoint.y, cell.y));
            maxPoint = v2i(Math.Max(maxPoint.x, cell.x), Math.Max(maxPoint.y, cell.y));
            hashSet.Add(cell);
        }

        public struct VerticalCellsEnumerable : IEnumerable<Vector2Int>
        {

            private CellFragment _src;
            private readonly int _x;
            // All cells are shifted by this while iteraion
            private Vector2Int _additionalShift;

            public VerticalCellsEnumerable(CellFragment src, int x, Vector2Int additionalShift)
            {
                _src = src;
                _x = x;
                _additionalShift = additionalShift;
            }


            /// <summary> Public non allocating enumerator. </summary>
            public VerticalCellsEnumerator GetEnumerator() => new VerticalCellsEnumerator(_src, _x, _additionalShift);

            public bool Contains(Vector2Int c) => _src.Contains(c - _additionalShift);

            // private methods for IEnumerable inheritance
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
            IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        }

        public struct VerticalCellsEnumerator : IEnumerator<Vector2Int>
        {
            private CellFragment _src;
            private readonly int _x;
            private HashSetVector2IntWrapper _srcIter;
            // All cells are shifted by this while iteraion
            private Vector2Int _additionalShift;


            private bool _movedNextInitially;
            private bool _noNext;
            private Vector2Int _next;

            public VerticalCellsEnumerator(CellFragment src, int x, Vector2Int additionalShift)
            {
                _src = src;
                _x = x;
                _srcIter = src.GetEnumerator();
                _movedNextInitially = false;
                _noNext = false;
                _next = default;
                _additionalShift = additionalShift;
            }

            public Vector2Int Current => !_movedNextInitially || _noNext
                ? throw new InvalidOperationException("No more elements to iterate!")
                : _next + _additionalShift;

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

            public bool MoveNext()
            {
                _noNext = !getNext(out _next);
                _movedNextInitially = true;
                return !_noNext;
            }

            private bool getNext(out Vector2Int next)
            {
                next = default;
                if (_src == null || _x > _src.Size.x || _x < 0 || _noNext)
                {
                    return false;
                }
                bool srcHasNext = _srcIter.MoveNext();
                while (srcHasNext)
                {
                    if (_srcIter.Current.x == _x)
                    {
                        next = _srcIter.Current;
                        return true;
                    }
                    srcHasNext = _srcIter.MoveNext();
                };
                return false;
            }

            void IEnumerator.Reset()
            {
                _srcIter = _src.GetEnumerator();
                _movedNextInitially = false;
                _noNext = false;
                _next = default;
            }

            public void Dispose() { }
        }


        private readonly struct FragmentCoupleKey : IEquatable<FragmentCoupleKey>
        {

            public readonly Vector2Int Distance;

            public readonly CellFragment Fragment1;
            public readonly CellFragment Fragment2;

            public FragmentCoupleKey(Vector2Int distance, CellFragment fragment1, CellFragment fragment2)
            {
                this.Distance = distance;
                this.Fragment1 = fragment1;
                this.Fragment2 = fragment2;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                if (obj is not FragmentCoupleKey)
                {
                    return false;
                }
                return Equals((FragmentCoupleKey)obj);
            }
            public bool Equals(FragmentCoupleKey other)
                => //other is not null &&
                Distance.Equals(other.Distance)
                && Fragment1.Equals(other.Fragment1)
                && Fragment2.Equals(other.Fragment2);

            public override int GetHashCode() => HashCode.Combine(Distance, Fragment1, Fragment2);

            public static bool operator ==(FragmentCoupleKey left, FragmentCoupleKey right)
                => EqualityComparer<FragmentCoupleKey>.Default.Equals(left, right);
            public static bool operator !=(FragmentCoupleKey left, FragmentCoupleKey right)
                => !(left == right);
        }
    }
}
