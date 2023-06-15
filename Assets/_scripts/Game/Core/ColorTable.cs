using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Core {

    /// <summary> Mutable color cell table. </summary>
    public class ColorTable : IEquatable<ColorTable> {

        CellColor?[,] cellsTable;

        private Vector2Int _size;

        public Vector2Int size => _size;

        /// <summary>
        /// Index of the first column in the cells table. This index is incremented on table scrolls left 
        /// while the whole 2D array of cells stays still.
        /// </summary>
        int startingX;

        // Array for tempopary storing neighbour cells during calculations.
        Cell[] neighbourCellsArray = new Cell[Direction.FOUR_DIRECTIONS.Length];

        public ColorTable(Vector2Int size) {
            _size = size;
            cellsTable = new CellColor?[size.x, size.y];
        }

        public ColorTable(Vector2Int size, IEnumerable<Cell> cells) : this(size) {
            foreach (var cell in cells) {
                this[cell.Position] = cell.Color;
            }
        }

        public ColorTable(ushort width, ushort height) : this(v2i(width, height)) { }

        // Defines the x,y indexer, which will allow colored cells
        public CellColor? this[int x, int y] {
            get {
                return getColor(v2i(x, y));
            }
            set {
                SetCell(new Vector2Int(x, y), value);
            }
        }

        // Defines the Vector2 indexer, which will allow colored cells
        public CellColor? this[Vector2Int position] {
            get {
                return getColor(position);
            }
            set {
                SetCell(position, value);
            }
        }

        public void SetCell(Vector2Int pos, CellColor? color) {
            verifyCellPosition(pos);
            Vector2Int shiftedPos = shiftPosition(pos);
            cellsTable[shiftedPos.x, shiftedPos.y] = color;
        }

        public void RemoveCell(Vector2Int pos) {
            verifyCellPosition(pos);
            Vector2Int shiftedPos = shiftPosition(pos);
            cellsTable[shiftedPos.x, shiftedPos.y] = null;
        }

        /// <summary>
        /// Finds cells which correspond a pattern for color at specified position of table.
        /// </summary>
        public uint FindPattern(
            ICellPatterns patternsBank,
            Vector2Int includeCell,
            Vector2Int[] matchedCellsBuffer
        ) {
            uint neighbourCellsCount = 0;
            foreach (Vector2Int dir in Direction.FOUR_DIRECTIONS) {
                if (!IsOutOfMapBounds(includeCell + dir)) {
                    CellColor? col = this[includeCell + dir];
                    if (col.HasValue) {
                        neighbourCellsArray[neighbourCellsCount++] = new Cell(includeCell + dir, col.Value);
                    }
                }
            }

            return FindPattern(patternsBank, includeCell, matchedCellsBuffer, neighbourCellsArray, neighbourCellsCount);
        }

        /// <summary>
        /// Finds cells which correspond a pattern for color at specified position of table.
        /// </summary>
        public uint FindPattern(
            ICellPatterns patternsBank,
            Vector2Int includeCell,
            Vector2Int[] matchedCellsBuffer,
            Cell[] neighbourCellsArray,
            uint neighboursCount
        ) {
            if (neighboursCount == 0) {
                return 0;
            }

            CellColor? cellColor = getColor(includeCell);
            if (!cellColor.HasValue) {
                for (int i = 0; i < neighboursCount; i++) {
                    Cell neighbour = neighbourCellsArray[i];
                    var matchedCount = FindPattern(
                        patternsBank[neighbour.Color], neighbour.Color, includeCell, matchedCellsBuffer
                    );
                    if (matchedCount > 0) {
                        return matchedCount;
                    }
                }
            } else {
                return FindPattern(patternsBank[cellColor.Value], cellColor.Value, includeCell, matchedCellsBuffer);
            }
            return 0;
        }

        public uint FindPattern(
            IEnumerable<CellFragment> patterns,
            CellColor color,
            Vector2Int withCellPos,
            Vector2Int[] outMatchedCells
        ) {
            int i = 0;
            foreach (var pattern in patterns) {
                int patternWidth = pattern.Size.x;
                int patternHeight = pattern.Size.y;

                Util.RectInt searchRect = getCellAroundSearchBounds(pattern, withCellPos);

                for (int shiftXLocal = searchRect.MinX; shiftXLocal <= searchRect.MaxX - patternWidth; shiftXLocal++) {
                    for (int shiftYLocal = searchRect.MinY; shiftYLocal <= searchRect.MaxY - patternHeight; shiftYLocal++) {
                        uint matchedCellsCount = checkForMatch(pattern, shiftXLocal, shiftYLocal, color, outMatchedCells, withCellPos);
                        if (matchedCellsCount == pattern.Count()) {
                            return matchedCellsCount;
                        }
                    }
                }
                i++;
            }
            return 0;
        }

        private Util.RectInt getCellAroundSearchBounds(CellFragment pattern, Vector2Int withCellPosLocal) {
            int patternWidth = pattern.Size.x;
            int patternHeight = pattern.Size.y;
            int xMin = Math.Max(0, withCellPosLocal.x - patternWidth);
            int xMax = Math.Min(size.x, withCellPosLocal.x + patternWidth);
            int yMin = Math.Max(0, withCellPosLocal.y - patternHeight);
            int yMax = Math.Min(size.y, withCellPosLocal.y + patternHeight);
            return new Util.RectInt(v2i(xMin, yMin), v2i(xMax - xMin, yMax - yMin));
        }

        private uint checkForMatch(CellFragment pattern, int shiftX, int shiftY, CellColor color,
            Vector2Int[] outMatchedCellsLocal, Vector2Int includeCell) {
            Vector2Int shift = v2i(shiftX, shiftY);
            uint matched = 0;
            foreach (var patternCell in pattern) {
                Vector2Int cell = shift + patternCell;
                if (getColor(cell) == color || cell == includeCell) {
                    outMatchedCellsLocal[matched++] = cell;
                } else {
                    return 0;
                }
            }
            return matched;
        }

        public void ScrollLeft(IEnumerable<CellColor?> newWallCells) {
            // Shift cells
            scrollMapLeft();
            // Add new wall
            spawnNewWall();
            //Debug.Log(this);

            void spawnNewWall() {
                int lastColumnX = size.x - 1;
                int y = 0;
                foreach (CellColor? cellColor in newWallCells) {
                    SetCell(v2i(lastColumnX, y++), cellColor);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOutOfHorizontalMapBounds(float x) => x < 0 || x >= size.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOutOfVerticalMapBounds(float y) => y < 0 || y >= size.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOutOfMapBounds(Vector2 position)
            => IsOutOfHorizontalMapBounds(position.x) || IsOutOfVerticalMapBounds(position.y);

        public override bool Equals(object obj) {
            return Equals(obj as ColorTable);
        }

        public bool Equals(ColorTable other) {
            if (other is null || !_size.Equals(other._size)) {
                return false;
            }

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    if (this[x, y] != other[x, y])
                        return false;
                }
            }
            return true;
        }

        public static bool operator ==(ColorTable left, ColorTable right) {
            return EqualityComparer<ColorTable>.Default.Equals(left, right);
        }

        public static bool operator !=(ColorTable left, ColorTable right) {
            return !(left == right);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ColorTable]");
            for (int y = size.y - 1; y >= 0; y--) {
                sb.Append("\n|");
                for (int x = 0; x < size.x; x++) {
                    sb.Append(colorToCharacter(this[x, y]));
                }
                sb.Append("|");
            }
            return sb.ToString();
        }

        /// <summary> Shifts all table cells to the left. </summary>
        private void scrollMapLeft() {
            // Moving all cells left by 1 cell.
            startingX++;
            if (startingX >= size.x) {
                startingX = 0;
            }
        }

        private CellColor? getColor(Vector2Int cell) {
            Vector2Int shiftedPosition = shiftPosition(cell);
            return cellsTable[shiftedPosition.x, shiftedPosition.y];
        }

        private char colorToCharacter(CellColor? cellColor) {
            if (cellColor.HasValue) {
                return cellColor.Value switch {
                    CellColor.Magenta => 'M',
                    CellColor.Blue => 'B',
                    CellColor.PaleBlue => 'b',
                    CellColor.Red => 'R',
                    CellColor.Orange => 'O',
                    CellColor.Green => 'G',
                    CellColor.Yellow => 'Y',
                    _ => throw new ArgumentException($"Unknown cell color: {nameof(cellColor)}")
                };
            } else {
                return ' ';
            }
        }

        private Vector2Int shiftPosition(Vector2Int position) {
            int shiftedX = (position.x + startingX) % size.x;
            Vector2Int shiftedPosition = v2i(shiftedX, position.y);
            return shiftedPosition;
        }

        private void verifyCellPosition(Vector2Int pos) {
            if (IsOutOfMapBounds(pos)) {
                throw new IndexOutOfRangeException(
                    $"Specified cell position {pos.ToString()} is out of the map bounds {size.ToString()}!"
                );
            }
        }
    }
}