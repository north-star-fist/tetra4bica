using UnityEngine;

namespace Tetra4bica.Core {

    // Cell generation strategy.
    public interface ICellGenerator {

        /// <summary> Generates new cells and fills passed array with them. </summary>
        void GenerateCells(CellColor?[] arrayToFill) {
            // Default implementation
            // TODO Extract it into strategy interface and store as Unity asset
            bool atLeastOneEmpty = false;
            for (int i = 0; i < arrayToFill.Length; i++) {
                if (Random.value < 0.5f) {
                    arrayToFill[i] = randomCell();
                } else {
                    arrayToFill[i] = null;
                    atLeastOneEmpty = true;
                }
            }
            if (!atLeastOneEmpty) {
                int cavityInd = Random.Range(0, arrayToFill.Length);
                arrayToFill[cavityInd] = null;
            }
        }

        private static CellColor randomCell() {
            return Cells.ALL_CELL_TYPES[Random.Range(0, Cells.ALL_CELL_TYPES.Length)];
        }

    }
}
