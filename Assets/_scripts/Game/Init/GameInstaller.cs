using Newtonsoft.Json;
using System.Collections.Generic;
using Tetra4bica.Core;
using Tetra4bica.Input;
using UniRx;
using UnityEngine;
using Zenject;
using static Tetra4bica.Core.GameLogic;

namespace Tetra4bica.Init {

    public class GameInstaller : MonoInstaller {

        [Header("Game Setup")]
        [Tooltip("Number of bricks that stay in one fullscreen vertical wall")]
        public int tunnelHeightCellCount = 10;
        [Tooltip("Length of the tunnel measured in bricks")]
        public int tunnelWidthCellCount = 20;
        [Tooltip("Delay between table cells scrolling by one cell left in seconds")]
        public float tableScrollTimeStep = 2f;
        [Tooltip("Position of the bottom left corner of player tetromino at the game start")]
        public Vector2Int playerStartPosition = new Vector2Int(0, 4);

        [Tooltip("Player tetromino color")]
        public CellColor playerColor;

        [Tooltip("Projectile speed in cells per second")]
        public float projectileSpeed = 5f;

        [Tooltip("Time delay after which the game will start automatically even if player did not touch the screen")]
        public float autoStartTime = 2f;

        [Tooltip("Projectiles are stopped touching bricks if this flag is on. Like cells are rubberish and brake projectiles")]
        public bool lateralBricksStopProjectiles = true;
        [Tooltip("Projectiles are stopped on the floor and ceiling collisions if this flag is on")]
        public bool projectilesCollideMapBounds = true;

        [Tooltip("Color of projectile that became part of a wall")]
        public CellColor frozenProjectileColor = CellColor.PaleBlue;

        [Tooltip("JSON File keeping cell patterns for elimination")]
        public string patternsFile = "cell_patterns";

        [Tooltip("Custom gaim input events provider (maybe recorded beforehead). " +
            "Implementation of IGameInputEventProvider should be added to the game object. " +
            "Use it as alternative for natural game input for Debug purposes.")]
        public CustomGameInputEventsProviderComponent customInputEventsProvider;

        [Tooltip("Custom new rightest column generator. " +
            "Implementation of ICellGenerator should be added to the game object. " +
            "Use it as alternative for natural game input for Debug purposes.")]
        public CustomCellColumnGeneratorComponent customCellGenerator;


        [Inject]
        PlayerInput playerInputSetup;

        public override void InstallBindings() {

            GameSettings gSettings = new GameSettings(
                tunnelWidthCellCount,
                tunnelHeightCellCount,
                tableScrollTimeStep,
                playerStartPosition,
                playerColor,
                frozenProjectileColor,
                projectileSpeed,
                autoStartTime,
                lateralBricksStopProjectiles,
                projectilesCollideMapBounds
            );
            Container.BindInstance(gSettings).AsSingle().NonLazy();

            if (customCellGenerator != null) {
                Container.BindInterfacesTo<CustomCellColumnGeneratorComponent>().FromComponentOn(customCellGenerator.gameObject)
                    .AsSingle().NonLazy();
            } else {
                Container.Bind<ICellGenerator>().FromInstance(new CellGenerator()).AsSingle().NonLazy();
            }

            Container.BindInterfacesAndSelfTo<GameUpdater>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            if (customInputEventsProvider != null) {
                Container.BindInterfacesTo<CustomGameInputEventsProviderComponent>()
                    .FromComponentOn(customInputEventsProvider.gameObject).AsSingle().NonLazy();
            } else {
                Container.BindInterfacesAndSelfTo<DefaultGameInputEventProvider>().AsSingle().NonLazy();
            }

            Container.BindInterfacesAndSelfTo<GameLogic>().AsSingle()
            .OnInstantiated(
                // Binding Game Logic and PlayerInput to switch ability to control game objects on and off
                (InjectContext context, GameLogic logic) => {
                    logic.GamePhaseStream.Subscribe(phase => {
                        if (phase is GamePhase.Started) {
                            playerInputSetup.Enable();
                        } else {
                            playerInputSetup.Disable();
                        }
                    });
                    // Do not process player input too early. Let game to start first.
                    playerInputSetup.Disable();
                }
            ).NonLazy();

            Container.Bind<ICellPatterns>().FromMethod(readCellPatternsFromFile).AsSingle().NonLazy();
        }

        private ICellPatterns readCellPatternsFromFile() {
            TextAsset patternsAsset = Resources.Load<TextAsset>(patternsFile);
            string patternsDictionaryJson = patternsAsset.text;
            var patternMap = JsonConvert.DeserializeObject<Dictionary<CellColor, IEnumerable<Vector2Int>>>(
                patternsDictionaryJson,
                new Vector2IntJsonConverter()
            );
            return new CellPatterns(patternMap);
        }

        class CellGenerator : ICellGenerator {
            // The class uses default interface cell generation
        }
    }
}