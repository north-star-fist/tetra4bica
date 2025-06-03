﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Tetra4bica.Core;
using Tetra4bica.Input;
using UniRx;
using UnityEngine;
using Zenject;
using static Tetra4bica.Core.GameLogic;

namespace Tetra4bica.Init
{

    public class GameInstaller : MonoInstaller
    {

        [Header("Game Setup")]
        [SerializeField, Tooltip("Number of bricks that stay in one fullscreen vertical wall")]
        private int _tunnelHeightCellCount = 10;
        [SerializeField, Tooltip("Length of the tunnel measured in bricks")]
        private int _tunnelWidthCellCount = 20;
        [SerializeField, Tooltip("Delay between table cells scrolling by one cell left in seconds")]
        private float _tableScrollTimeStep = 2f;
        [SerializeField, Tooltip("Position of the bottom left corner of player tetromino at the game start")]
        private Vector2Int _playerStartPosition = new Vector2Int(0, 4);

        [SerializeField, Tooltip("Player tetromino color")]
        private CellColor _playerColor;

        [SerializeField, Tooltip("Projectile speed in cells per second")]
        private float _projectileSpeed = 5f;

        [
            SerializeField,
            Tooltip("Projectiles are stopped touching bricks if this flag is on. " +
            "Like cells are rubberish and brake projectiles")
        ]
        private bool _lateralBricksStopProjectiles = true;
        [SerializeField, Tooltip("Projectiles are stopped on the floor and ceiling collisions if this flag is on")]
        private bool _projectilesCollideMapBounds = true;

        [SerializeField, Tooltip("Color of projectile that became part of a wall")]
        private CellColor _frozenProjectileColor = CellColor.PaleBlue;

        [SerializeField, Tooltip("JSON File keeping cell patterns for elimination")]
        private string _patternsFile = "cell_patterns";

        [
            SerializeField,
            Tooltip("Custom gaim input events provider (maybe recorded beforehead). " +
                "Implementation of IGameInputEventProvider should be added to the game object. " +
                "Use it as alternative for natural game input for Debug purposes.")
        ]
        private CustomGameInputEventsProviderComponent _customInputEventsProvider;

        [
            SerializeField,
            Tooltip("Custom new rightest column generator. " +
                "Implementation of ICellGenerator should be added to the game object. " +
                "Use it as alternative for natural game input for Debug purposes.")
        ]
        private CustomCellColumnGeneratorComponent _customCellGenerator;


        [Inject]
        private PlayerInput _playerInputSetup;


        public override void InstallBindings()
        {

            GameSettings gSettings = new GameSettings(
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
            Container.BindInstance(gSettings).AsSingle().NonLazy();

            if (_customCellGenerator != null)
            {
                Container.BindInterfacesTo<CustomCellColumnGeneratorComponent>().FromComponentOn(_customCellGenerator.gameObject)
                    .AsSingle().NonLazy();
            }
            else
            {
                Container.Bind<ICellGenerator>().FromInstance(new CellGenerator()).AsSingle().NonLazy();
            }

            Container.BindInterfacesAndSelfTo<GameUpdater>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            if (_customInputEventsProvider != null)
            {
                Container.BindInterfacesTo<CustomGameInputEventsProviderComponent>()
                    .FromComponentOn(_customInputEventsProvider.gameObject).AsSingle().NonLazy();
            }
            else
            {
                Container.BindInterfacesAndSelfTo<DefaultGameInputEventProvider>().AsSingle().NonLazy();
            }

            Container.BindInterfacesAndSelfTo<GameLogic>().AsSingle()
            .OnInstantiated(
                // Binding Game Logic and PlayerInput to switch ability to control game objects on and off
                (InjectContext context, GameLogic logic) =>
                {
                    logic.GamePhaseStream.Subscribe(phase =>
                    {
                        if (phase is GamePhase.Started)
                        {
                            _playerInputSetup.Enable();
                        }
                        else
                        {
                            _playerInputSetup.Disable();
                        }
                    });
                    // Do not process player input too early. Let game to start first.
                    _playerInputSetup.Disable();
                }
            ).NonLazy();

            Container.Bind<ICellPatterns>().FromMethod(readCellPatternsFromFile).AsSingle().NonLazy();
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
    }
}
