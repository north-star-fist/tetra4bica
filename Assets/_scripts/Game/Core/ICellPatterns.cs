namespace Tetra4bica.Core {
    public interface ICellPatterns {

        /// <summary> Gets cell patterns for particular color. </summary>
        public CellFragment[] GetPatterns(CellColor color);

        /// <summary> Gets cell patterns for particular color. </summary>
        public CellFragment[] this[CellColor color] => GetPatterns(color);

    }
}