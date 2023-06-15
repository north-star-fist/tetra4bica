using System.ComponentModel;
using Tetra4bica.Util;

namespace Tetra4bica.Core {

    /// <summary> Dictionary of tetromino patterns with their colours. </summary>
    public class TetrominoPatterns : ICellPatterns {

        private static readonly bool[,] SQUARE = new bool[,] {
            { true, true },
            { true, true }
        };
        private static readonly bool[,] STICK = new bool[,] {
            { true, true, true, true }
        };
        private static readonly bool[,] T = new bool[,] {
            { false, true },
            { true, true },
            { false, true }
        };
        private static readonly bool[,] S = new bool[,] {
            { true, false },
            { true, true },
            { false, true }
        };
        private static readonly bool[,] Z = new bool[,] {
            { false, true },
            { true, true },
            { true, false }
        };
        private static readonly bool[,] L = new bool[,] {
            { true, true, true },
            { true, false, false }
        };
        private static readonly bool[,] MIRRORED_L = new bool[,] {
            { true, false, false },
            { true, true, true },
        };

        /// <summary>
        /// Rectangle pattern.
        /// Rectangle has one form only. It stays the same being rotated.
        /// </summary>
        public readonly CellFragment[] SQUARE_PATTERNS = new CellFragment[] {
            CellFragment.Fragment(SQUARE, out var _)
        };

        /// <summary> Stick patterns. Vertical and horizontal one. </summary>
        public readonly CellFragment[] STICK_PATTERNS;


        /// <summary>
        /// T patterns. T, T rotated clockwise by 90 degrees, T rotated by 180 degrees
        /// and T rotated counter-clockwise by 90 degrees
        /// </summary>
        public readonly CellFragment[] T_PATTERNS;


        // S
        public readonly CellFragment[] S_PATTERNS;


        // Z
        public readonly CellFragment[] Z_PATTERNS;


        // L
        public readonly CellFragment[] L_PATTERNS;


        // Back L
        public readonly CellFragment[] MIRRORED_L_PATTERNS;

        public TetrominoPatterns() {
            STICK_PATTERNS = new CellFragment[] {
                CellFragment.Fragment(STICK, out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy90(STICK, true), out var _)
            };
            T_PATTERNS = new CellFragment[] {
                CellFragment.Fragment(T, out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy90(T, true), out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy180(T), out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy90(T, false), out var _),
            };
            S_PATTERNS = new CellFragment[] {
                CellFragment.Fragment(S, out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy90(S, true), out var _)
            };
            Z_PATTERNS = new CellFragment[]  {
                CellFragment.Fragment(Z, out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy90(Z, true), out var _)
            };
            L_PATTERNS = new CellFragment[] {
                CellFragment.Fragment(L, out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy90(L, true), out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy180(L), out var _),
                CellFragment.Fragment(MatrixUtil.RotateBy90(L, false), out var _)
            };
            MIRRORED_L_PATTERNS = new CellFragment[] {
                CellFragment.Fragment(MIRRORED_L, out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy90(MIRRORED_L, true), out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy180(MIRRORED_L), out var _),
                CellFragment.Fragment( MatrixUtil.RotateBy90(MIRRORED_L, false), out var _)
            };
        }

        public CellFragment[] GetPatterns(CellColor color) => color switch {
            CellColor.Yellow => SQUARE_PATTERNS,
            CellColor.Red => Z_PATTERNS,
            CellColor.Green => S_PATTERNS,
            CellColor.Blue => MIRRORED_L_PATTERNS,
            CellColor.PaleBlue => STICK_PATTERNS,
            CellColor.Magenta => T_PATTERNS,
            CellColor.Orange => L_PATTERNS,
            _ => throw new InvalidEnumArgumentException("Unknown color!", (int)color, typeof(CellColor))
        };

        public CellFragment[] this[CellColor color] => GetPatterns(color);

    }
}