using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Tetra4bica.Core;
using Tetra4bica.Util;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Tetra4bica.Init
{

    [CreateAssetMenu(
        fileName = "Game Settings Installer",
        menuName = "Tetra4bica/DI Installers/Game Settings Installer"
    )]
    public class GameInstaller : AScriptableInstaller
    {

        [Header("Game Setup")]
        [
            SerializeField,
            Tooltip("Number of bricks that stay in one fullscreen vertical wall"),
            FormerlySerializedAs("tunnelHeightCellCount")
        ]
        private int _tunnelHeightCellCount = 10;
        [
            SerializeField,
            Tooltip("Length of the tunnel measured in bricks"),
            FormerlySerializedAs("tunnelWidthCellCount")
        ]
        private int _tunnelWidthCellCount = 20;
        [
            SerializeField,
            Tooltip("Delay between table cells scrolling by one cell left in seconds"),
            FormerlySerializedAs("tableScrollTimeStep")
        ]
        private float _tableScrollTimeStep = 2f;
        [
            SerializeField,
            Tooltip("Position of the bottom left corner of player tetromino at the game start"),
            FormerlySerializedAs("playerStartPosition")
        ]
        private Vector2Int _playerStartPosition = new Vector2Int(0, 4);

        [SerializeField, Tooltip("Player tetromino color"), FormerlySerializedAs("playerColor")]
        private CellColor _playerColor;

        [SerializeField, Tooltip("Projectile speed in cells per second"), FormerlySerializedAs("projectileSpeed")]
        private float _projectileSpeed = 5f;

        [
            SerializeField,
            Tooltip("Projectiles are stopped touching bricks if this flag is on. " +
                "Like cells are rubberish and brake projectiles"),
            FormerlySerializedAs("lateralBricksStopProjectiles")
        ]
        private bool _lateralBricksStopProjectiles = true;
        [
            SerializeField,
            Tooltip("Projectiles are stopped on the floor and ceiling collisions if this flag is on"),
            FormerlySerializedAs("projectilesCollideMapBounds")
        ]
        private bool _projectilesCollideMapBounds = true;

        [
            SerializeField,
            Tooltip("Color of projectile that became part of a wall"),
            FormerlySerializedAs("frozenProjectileColor")
        ]
        private CellColor _frozenProjectileColor = CellColor.PaleBlue;

        [
            SerializeField,
            Tooltip("JSON File keeping cell patterns for elimination"),
            FormerlySerializedAs("patternsFile")
        ]
        private string _patternsFile = "cell_patterns";


        public override void Install(IContainerBuilder builder)
        {
            GameLogic.GameSettings settings = new GameLogic.GameSettings(
                _tunnelWidthCellCount,
                _tunnelHeightCellCount,
                _tableScrollTimeStep,
                _playerStartPosition,
                _playerColor,
                _frozenProjectileColor,
                _projectileSpeed,
                _lateralBricksStopProjectiles,
                _projectilesCollideMapBounds
            );
            builder.RegisterInstance(settings);

            builder.RegisterInstance(readCellPatternsFromFile());

            builder.RegisterInstance(new CellGenerator()).AsImplementedInterfaces();
        }

        private ICellPatterns readCellPatternsFromFile()
        {
            TextAsset patternsAsset = Resources.Load<TextAsset>(_patternsFile);
            string patternsDictionaryJson = patternsAsset.text;
            var patternMap = JsonConvert.DeserializeObject<Dictionary<CellColor, IEnumerable<Vector2Int>>>(
                patternsDictionaryJson,
                new Vector2IntJsonConverter()
            );
            return new CellPatterns(patternMap);
        }

        class CellGenerator : ICellGenerator
        {
            // The class uses default interface cell generation
        }

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
}
