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

        readonly HashSet<Vector2Int> cellsLocal;

        int cellsSetHash;
        bool isCellsSetHashCached;

        // Shift to the first non-zero cell. It is kept not to run through all cells twice while object instantiating.
        private Vector2Int cellsShift;


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
            cellsLocal = (cells == null) || cells.Count == 0 ? null : cells;
            this.Size = size;
            cellsSetHash = 0;
            isCellsSetHashCached = false;
            this.cellsShift = cellsShift;
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

        public uint Count() => (uint)(cellsLocal != null ? cellsLocal.Count : 0);

        public bool IsEmpty() => Size == Vector2Int.zero;

        public bool Contains(Vector2Int cell) => cellsLocal != null && cellsLocal.Contains(cell - cellsShift);


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
            => cellsLocal == null ? EMPTY_FRAGMENT_ENUMERATOR : new HashSetVector2IntWrapper(cellsLocal, cellsShift + shift);

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
            if (other.cellsLocal == null)
            {
                return cellsLocal == null;
            }
            if (Size != other.Size)
            {   // Rect bounds
                return false;
            }
            if (Count() != other.Count())
            { // Non empty cells count
                return false;
            }
            foreach (var cell in cellsLocal)
            {
                if (!other.cellsLocal.Contains(cell + cellsShift - other.cellsShift))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            if (isCellsSetHashCached)
            {
                cellsSetHash = calculateCellsHash();
                isCellsSetHashCached = true;
            }
            return cellsSetHash;
        }

        int calculateCellsHash()
        {
            int hash = 0;
            foreach (var cell in cellsLocal)
            {
                hash += hash * -1521134295 + (cell + cellsShift).GetHashCode();
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

            CellFragment src;
            readonly int x;
            // All cells are shifted by this while iteraion
            Vector2Int additionalShift;

            public VerticalCellsEnumerable(CellFragment src, int x, Vector2Int additionalShift)
            {
                this.src = src;
                this.x = x;
                this.additionalShift = additionalShift;
            }


            /// <summary> Public non allocating enumerator. </summary>
            public VerticalCellsEnumerator GetEnumerator() => new VerticalCellsEnumerator(src, x, additionalShift);

            public bool Contains(Vector2Int c) => src.Contains(c - additionalShift);

            // private methods for IEnumerable inheritance
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
            IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

        }

        public struct VerticalCellsEnumerator : IEnumerator<Vector2Int>, IEnumerator, IDisposable
        {
            private CellFragment src;
            private int x;
            private HashSetVector2IntWrapper srcIter;
            // All cells are shifted by this while iteraion
            Vector2Int additionalShift;


            bool movedNextInitially;
            bool noNext;
            Vector2Int next;

            public VerticalCellsEnumerator(CellFragment src, int x, Vector2Int additionalShift)
            {
                this.src = src;
                this.x = x;
                this.srcIter = src.GetEnumerator();
                movedNextInitially = false;
                noNext = false;
                next = default;
                this.additionalShift = additionalShift;
            }

            public Vector2Int Current => !movedNextInitially || noNext
                ? throw new InvalidOperationException("No more elements to iterate!")
                : next + additionalShift;

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

            public bool MoveNext()
            {
                noNext = !getNext(out next);
                movedNextInitially = true;
                return !noNext;
            }

            private bool getNext(out Vector2Int next)
            {
                next = default;
                if (src == null || x > src.Size.x || x < 0 || noNext)
                {
                    return false;
                }
                bool srcHasNext = srcIter.MoveNext();
                while (srcHasNext)
                {
                    if (srcIter.Current.x == x)
                    {
                        next = srcIter.Current;
                        return true;
                    }
                    srcHasNext = srcIter.MoveNext();
                };
                return false;
            }

            void IEnumerator.Reset()
            {
                srcIter = src.GetEnumerator();
                movedNextInitially = false;
                noNext = false;
                next = default;
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
