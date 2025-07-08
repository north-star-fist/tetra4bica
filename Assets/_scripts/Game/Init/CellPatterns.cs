using System.Collections.Generic;
using System.Linq;
using Tetra4bica.Core;
using Tetra4bica.Util;
using UnityEngine;

namespace Tetra4bica.Init
{
    public class CellPatterns : ICellPatterns
    {

        private readonly Dictionary<CellColor, CellFragment[]> _patterns;

        public CellPatterns(IDictionary<CellColor, CellFragment> patternMap)
        {
            _patterns = patternMap.Aggregate(
                new Dictionary<CellColor, CellFragment[]>(),
                (patternBank, newPattern) =>
                {
                    patternBank[newPattern.Key] = getAllDirectionsPatterns(newPattern.Value);
                    return patternBank;
                }
            );
        }

        public CellPatterns(IDictionary<CellColor, IEnumerable<Vector2Int>> patternMap) :
            this(patternMap.Aggregate(new Dictionary<CellColor, CellFragment>(), (map, colCells) =>
            {
                map.Add(colCells.Key, CellFragment.Fragment(colCells.Value, out var _));
                return map;
            }))
        { }

        private CellFragment[] getAllDirectionsPatterns(CellFragment pattern)
        {
            return Enumerable.Range(0, 4).Aggregate(new List<CellFragment>() { pattern }, (list, _) =>
            {
                list.Add(
                    CellFragment.Fragment(MatrixUtil.RotateBy90(list.Last(), true), out var _)
                );
                return list;
            }).Distinct().ToArray();
        }

        public CellFragment[] GetPatterns(CellColor color)
        {
            return _patterns[color];
        }
    }
}
