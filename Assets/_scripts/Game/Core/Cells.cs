using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Tetra4bica.Core {

    /// <summary> Basic thing in the game. The cell of game field. </summary>
    public readonly struct Cell : IEquatable<Cell> {

        public readonly Vector2Int Position;
        public readonly CellColor Color;


        public Cell(Vector2Int position, CellColor color) {
            this.Position = position;
            this.Color = color;
        }

        public override bool Equals(object obj) {
            if (obj is Cell) {
                return Equals((Cell)obj);
            } else {
                return false;
            }
        }
        public bool Equals(Cell other) => Position.Equals(other.Position) && Color == other.Color;

        public override int GetHashCode() {
            int hashCode = -2056440846;
            hashCode = hashCode * -1521134295 + Position.GetHashCode();
            hashCode = hashCode * -1521134295 + ((int)Color);
            return hashCode;
        }

        public static bool operator ==(Cell left, Cell right) => EqualityComparer<Cell>.Default.Equals(left, right);
        public static bool operator !=(Cell left, Cell right) => !(left == right);

        public override readonly string ToString() => $"{Position.ToString()}:{nameof(Color)}";
    }

    public enum CellColor {
        Orange = 0,     // L
        Red = 1,        // S
        Green = 2,      // Z
        Blue = 3,       // Back L
        PaleBlue = 4,   // I
        Magenta = 5,     // T
        Yellow = 6,     // []
    }

    public static class Cells {
        static readonly Color orange = Color.Lerp(Color.yellow, Color.red, 0.5f);

        /// <summary> Enumeration of all cell colors. </summary>
        public static readonly CellColor[] ALL_CELL_TYPES = (CellColor[])Enum.GetValues(typeof(CellColor));

        /// <summary> Maps <see cref="CellColor"/> to Unity <see cref="Color"/>. </summary>
        public static Color ToUnityColor(CellColor cell) {
            // Color mapper
            return cell switch {
                CellColor.Red => Color.red,
                CellColor.Green => Color.green,
                CellColor.Blue => Color.blue,
                CellColor.PaleBlue => Color.cyan,
                CellColor.Magenta => Color.magenta,
                CellColor.Yellow => Color.yellow,
                CellColor.Orange => orange,
                _ => throw new InvalidEnumArgumentException("Unknown Cell type", (int)cell, typeof(CellColor))
            };
        }
    }
}