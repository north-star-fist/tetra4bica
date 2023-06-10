using Sergei.Safonov.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine;
using Zenject;
using static Sergei.Safonov.Utility.VectorExt;
using static Tetra4bica.Input.PlayerInput;
using static Tetra4bica.Input.PlayerInput.MovementInput;

namespace Tetra4bica.Core {

    /// <summary>
    /// Class that gets game input streams and gives game change streams.
    /// </summary>
    [ZenjectAllowDuringValidation]
    public class GameLogic : IGameEvents {

        private GameSettings gameSettings;
        private ICellPatterns cellPatterns;
        private ICellGenerator cellGenerator;
        public GameState gameState;

        #region Game event streams
        IObservable<float> _frameUpdateStream;
        public IObservable<float> FrameUpdateStream => _frameUpdateStream;

        IObservable<IEnumerable<CellColor>> _tableScrollStream;
        /// <summary>
        /// Scrolls game table cells left for one column passing new the most right column of the table.
        /// </summary>
        public IObservable<IEnumerable<CellColor>> TableScrollStream => _tableScrollStream;

        private Subject<Vector2Int> _gameStartedStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> GameStartedStream => _gameStartedStream.AsObservable();

        Subject<Cell> _newCellStream = new Subject<Cell>();
        public IObservable<Cell> NewCellStream => _newCellStream.AsObservable();

        Subject<PlayerTetromino> _playerTetrominoStream = new Subject<PlayerTetromino>();
        public IObservable<PlayerTetromino> PlayerTetrominoStream => _playerTetrominoStream.AsObservable();

        Subject<Vector2Int> _shotStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> ShotStream => _shotStream.AsObservable();

        Subject<bool> _rotationStream = new Subject<bool>();
        public IObservable<bool> RotationStream => _rotationStream.AsObservable();

        Subject<Cell> _eliminatedBricksStream = new Subject<Cell>();
        public IObservable<Cell> EliminatedBricksStream => _eliminatedBricksStream.AsObservable();

        Subject<Vector2> _projectileCoordinatesStream = new Subject<Vector2>();
        public IObservable<Vector2> ProjectileCoordinatesStream => _projectileCoordinatesStream;

        Subject<Vector2Int> _frozenProjectilesStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> FrozenProjectilesStream => _frozenProjectilesStream;

        Subject<uint> _scoreStream = new Subject<uint>();
        public IObservable<uint> ScoreStream => _scoreStream;

        Subject<GamePhase> _gamePhaseStream = new Subject<GamePhase>();
        public IObservable<GamePhase> GamePhaseStream => _gamePhaseStream;

        #endregion

        IGameInputBus playerInputBus = new GameInputStreams();
        IGameTimeBus timeEventsBus = new TimeEventBus();

        // Private inner streams
        /// <summary> Frozen projectiles inner stream. </summary>
        Subject<Vector2Int> _frozenProjectilesInnerStream = new Subject<Vector2Int>();
        // The stream of events when the whole wall shouldbeeliminated. Need to be combined with frame stream to
        // eliminate wall when it isupdated on UI.
        Subject<(int, Vector2Int)> _wallEliminationStream = new Subject<(int, Vector2Int)>();

        // Buffer for holding cells that match some pattern. Assuming there is no cell patterns with more than 16 cells area.
        Vector2Int[] matchedCellsBuffer = new Vector2Int[16];

        // Buffer for new wall cells.
        CellColor[] wallSpawnBuffer;

        // Buffer array for finding colorislands (used for counting cells in color region for making new cell color decision)
        bool[,] islandsBuffer;

        // Array for tempopary storing neighbour cells during calculations.
        Cell[] neighbourCellsArray = new Cell[Direction.FOUR_DIRECTIONS.Length];

        // Not to record game over before event that bring us here waiting for one frame
        private bool handleGameOverNextFrame;

        public struct GameSettings {
            readonly public int mapWidth;
            readonly public int mapHeight;
            readonly public float scrollTimeStep;
            readonly public Vector2Int playerStartPosition;
            readonly public CellColor playerColor;
            readonly public CellColor frozenProjectileColor;
            readonly public float projectileSpeed;
            readonly public float autoStartTime;
            readonly public bool lateralBricksStopProjectiles;
            readonly public bool projectilesCollideMapBounds;

            public GameSettings(
                int mapWidth,
                int mapHeight,
                float scrollTimeStep,
                Vector2Int playerStartPosition,
                CellColor playerColor,
                CellColor frozenProjectileColor,
                float projectileSpeed,
                float autoStartTime,
                bool lateralBricksStopProjectiles,
                bool projectilesCollideMapBounds
            ) {
                this.mapWidth = mapWidth;
                this.mapHeight = mapHeight;
                this.scrollTimeStep = scrollTimeStep;
                this.playerStartPosition = playerStartPosition;
                this.playerColor = playerColor;
                this.frozenProjectileColor = frozenProjectileColor;
                this.projectileSpeed = projectileSpeed;
                this.autoStartTime = autoStartTime;
                this.lateralBricksStopProjectiles = lateralBricksStopProjectiles;
                this.projectilesCollideMapBounds = projectilesCollideMapBounds;
            }
        }

        [Inject]
        public GameLogic(
            GameSettings gameSettings,
            IGameInputEventProvider inputEventProvider,
            ICellPatterns tetraminoPatterns,
            ICellGenerator cellGenerator
        ) {
            this.gameSettings = gameSettings;
            this.cellPatterns = tetraminoPatterns;
            this.cellGenerator = cellGenerator;
            wallSpawnBuffer = new CellColor[gameSettings.mapHeight];

            _frameUpdateStream = timeEventsBus.FrameUpdatePublisher;

            IObservable<int> scrollStepStream = _frameUpdateStream
                .Where(_ => gameState.gamePhase is not GamePhase.Paused and not GamePhase.NotStarted)
                .Scan((acc, delta) => acc + delta)
                .Select(time => (int)(time / gameSettings.scrollTimeStep))
                .DistinctUntilChanged()
                .Scan((0, 0), (oldStepsAndDelta, newSteps)
                    => { return (newSteps, newSteps - oldStepsAndDelta.Item1); })
                .SelectMany(stepsWithDelta => Enumerable.Range(stepsWithDelta.Item1 - stepsWithDelta.Item2, stepsWithDelta.Item2));
            // Making TableScrollStream connectable Observable not to repeat side effects for each subscriber
            // (as new column generation).
            _tableScrollStream = scrollStepStream
                .Do(_ => this.cellGenerator.GenerateCells(wallSpawnBuffer))
                .Select(steps => wallSpawnBuffer).Share();

            inputEventProvider.GetInputStream().Subscribe(e => e.Apply(timeEventsBus, playerInputBus));

            _frameUpdateStream.Where(_ => gameState.gamePhase is not GamePhase.Paused or GamePhase.NotStarted)
                .Subscribe(deltaTime => {
                    handleProjectiles(deltaTime);
                });
            _frameUpdateStream.Subscribe(_ => {
                if (handleGameOverNextFrame) {
                    handleGameOverNextFrame = false;
                    handleGameOver();
                }
            });


            TableScrollStream.Subscribe(newWall => {
                scrollBricks(newWall);
                checkWallsAroundPlayerTetramino();
            });

            // Running elimination of wall on thefollowing frame update because when table scrolls onto player tetramino
            // View should be updated first, and then exploded
            _wallEliminationStream.SelectMany(
                wData => _frameUpdateStream.First().Select(_ => wData))
                .Subscribe(tpl => eliminateWall(tpl.Item1, tpl.Item2));

            playerInputBus.PlayerMovementStream.Subscribe(movePlayer);
            playerInputBus.PlayerRotateStream.Subscribe(handleRotation);
            playerInputBus.PlayerShotStream.Subscribe(_ => handleShot());

            _frozenProjectilesInnerStream.Subscribe(handleNewCell);
            playerInputBus.GameStartStream
#if !UNITY_EDITOR
            // Autostart is added only in production not to bother Zenject validation
            .Merge(new[] {
                // Autostart after splash screen animation
                Observable.Timer(TimeSpan.FromSeconds(gameSettings.autoStartTime)).AsUnitObservable()
                .Where(_ => gameState.gamePhase == GamePhase.NotStarted)
            })
#endif
            .Subscribe(tableSize => SetPhase(GamePhase.Started));
            playerInputBus.GamePauseResumeStream.Subscribe(
                paused => SetPhase(paused ? GamePhase.Paused : GamePhase.Started)
            );


            resetGameState();
        }

        /// <summary> Sets new game phase. </summary>
        /// <returns>old phase</returns>
        GamePhase SetPhase(GamePhase gamePhase) {
            var oldPhase = gameState.gamePhase;
            if (gamePhase == GamePhase.Started) {
                if (gameState.gamePhase is GamePhase.NotStarted or GamePhase.GameOver) {
                    StartNewGame();
                } else if (gameState.gamePhase is GamePhase.Paused) {
                    // unpause
                }
            }
            gameState.gamePhase = gamePhase;
            _gamePhaseStream.OnNext(gamePhase);
            //Debug.Log($"Switching {oldPhase} to {gamePhase}");
            return oldPhase;
        }

        void StartNewGame() {
            resetGameState();
            islandsBuffer = new bool[gameSettings.mapWidth, gameSettings.mapHeight];
            _gameStartedStream.OnNext(new Vector2Int(gameSettings.mapWidth, gameSettings.mapHeight));
            _playerTetrominoStream.OnNext(gameState.playerTetromino);
        }

        void scrollBricks(IEnumerable<CellColor> newWall) {
            if (gameState.gamePhase is not GamePhase.GameOver) {
                if (!checkPlayerTetrominoCollisions(gameState.playerTetromino, Vector2Int.right)
                    || isNewColumnCollidingPlayer(newWall)) {
                    handleGameOver();
                }
            }
            gameState.gameTable.ScrollLeft(newWall);

            bool isNewColumnCollidingPlayer(IEnumerable<CellColor> newWall) {
                int y = 0;
                foreach (CellColor cellColor in newWall) {
                    if (cellColor is not CellColor.NONE
                        && gameState.playerTetromino.Contains(v2i(gameState.gameTable.size.x - 1, y))
                    ) {
                        return true;
                    }
                    y++;
                }
                return false;
            }
        }

        private void resetGameState() {
            gameState = new GameState(
                new PlayerTetromino(gameSettings.playerStartPosition, gameSettings.playerColor),
                // Doubled quantity of table cells should be enough for keeping all particles on the screen
                new Projectile[gameSettings.mapWidth * gameSettings.mapHeight * 2],
                GamePhase.NotStarted,
                new ColorTable(v2i(gameSettings.mapWidth, gameSettings.mapHeight))
            );
        }

        void movePlayer(MovementInput input) {
            if (gameState.gamePhase is not GamePhase.Started) { return; }

            Vector2Int dir = getMoveDirection(input);
            if (!checkPlayerTetrominoCollisions(gameState.playerTetromino, dir)) {
                handleGameOverNextFrame = true;
                return;
            }
            gameState.playerTetromino =
                gameState.playerTetromino.withPosition(calculateNewPlayerPosition(dir));
            _playerTetrominoStream.OnNext(gameState.playerTetromino);
            checkWallsAroundPlayerTetramino();
        }

        private void handleRotation(bool clockwise) {
            if (gameState.gamePhase is not GamePhase.Started) { return; }

            PlayerTetromino rotatedTetramino = gameState.playerTetromino.Rotate(clockwise);
            rotatedTetramino = fixPositionAfterRotation(rotatedTetramino);
            if (!checkPlayerTetrominoCollisions(rotatedTetramino)) {
                handleGameOverNextFrame = true;
                return;
            }
            gameState.playerTetromino = rotatedTetramino;

            gameState.playerTetromino = fixPositionAfterRotation(gameState.playerTetromino);
            _playerTetrominoStream.OnNext(gameState.playerTetromino);
            _rotationStream.OnNext(clockwise);

            // Arranges tetramino position if it exeeded map bounds after rotation
            PlayerTetromino fixPositionAfterRotation(PlayerTetromino tetramino) {
                var size = tetramino.Size;
                var position = tetramino.Position;
                Vector2Int fixedPosition = new Vector2Int(
                    Mathf.Clamp(position.x, 0, gameState.gameTable.size.x - size.x),
                    Mathf.Clamp(position.y, 0, gameState.gameTable.size.y - size.y)
                );
                return tetramino.withPosition(fixedPosition);
            }
        }

        private void handleShot() {
            if (gameState.gamePhase is not GamePhase.Started) { return; }

            _shotStream.OnNext(gameState.playerTetromino.direction);

            Vector2Int projectileStartPosition = gameState.playerTetromino.Position
                + gameState.playerTetromino.muzzle;

            if (
                !isOutOfMapBounds(projectileStartPosition)
                && gameState.gameTable[projectileStartPosition] is CellColor.NONE
            ) {
                gameState.projectiles[gameState.nextProjectileInd++] =
                    new Projectile(projectileStartPosition, gameState.playerTetromino.direction);
                if (gameState.nextProjectileInd >= gameState.projectiles.Length) {
                    gameState.nextProjectileInd = 0;
                }
            }
        }

        void handleProjectiles(float deltaTime) {
            if (gameState.gamePhase is GamePhase.Paused) { return; };

            for (int i = 0; i < gameState.projectiles.Length; i++) {
                Projectile projectile = gameState.projectiles[i];
                if (projectile.active) {
                    bool flewAway = gameSettings.projectilesCollideMapBounds
                        ? isOutOfHorizontalMapBounds(projectile.position.x)
                        : isOutOfMapBounds(projectile.position);
                    if (!flewAway) {
                        if (shouldBeFrozen(projectile, deltaTime, out Vector2Int landPosition)) {
                            _frozenProjectilesInnerStream.OnNext(landPosition);
                            projectile.active = false;
                        }
                    } else {
                        projectile.active = false;
                    }
                    if (projectile.active) { //still
                        projectile.position = projectile.position
                            + projectile.direction.toVector2() * gameSettings.projectileSpeed * deltaTime;
                        _projectileCoordinatesStream.OnNext(projectile.position);
                    }
                    // Writing updated projectile back to the array
                    gameState.projectiles[i] = projectile;
                }
            }
        }

        private bool shouldBeFrozen(Projectile projectile, float deltaTime, out Vector2Int landPosition) {
            landPosition = default;
            // checking integer position
            Vector2Int intPos = projectile.position.toVector2Int();
            if ((gameSettings.projectilesCollideMapBounds && isOutOfHorizontalMapBounds(intPos.x))
                || isOutOfMapBounds(intPos)
            ) {
                return false;
            }

            // Checking each cell passed by projectile to decide does it collide with anything
            // If The prijectile is already above occupied cell then backpress the projectile back to freeze it in 
            // free space. During backpressing we should check does the projectile collides with the player tetromino
            // itself. In this case player tetromino goes down (game over)
            if (gameState.gameTable[projectile.position.toVector2Int()] is not CellColor.NONE) {
                // projectile cell is occupied already. Projectile should be backpressured
                return backPressureProjectile(projectile, out landPosition);
            }
            var farthestPosition = projectile.position + projectile.direction.toVector2() * gameSettings.projectileSpeed * deltaTime;
            Vector2Int posDelta = (farthestPosition - projectile.position.toVector2Int()).toVector2Int();
            for (int passedCells = 0; (passedCells * projectile.direction).sqrMagnitude <= posDelta.sqrMagnitude; passedCells++) {
                Vector2Int newPosition =
                    (projectile.position + (passedCells * projectile.direction)).toVector2Int();
                if (shouldBeFrozen(projectile.direction, newPosition, out landPosition)) {
                    return true;
                }
            }
            return false;

            bool shouldBeFrozen(Vector2Int direction, Vector2Int newPosition, out Vector2Int landPosition) {
                landPosition = newPosition;
                // Check rubberish neighbours
                if (gameSettings.lateralBricksStopProjectiles &&
                    (checkNeighbourCell(newPosition, Vector2Int.left)
                    || checkNeighbourCell(newPosition, Vector2Int.right)
                    || checkNeighbourCell(newPosition, Vector2Int.down)
                    || checkNeighbourCell(newPosition, Vector2Int.up))
                ) {
                    return true;
                }
                // Check cell in front of the projectile
                if (checkNeighbourCell(newPosition, direction)) {
                    return true;
                }
                //Check borders
                return stoppedByMapBounds(newPosition + direction);

                bool checkNeighbourCell(Vector2Int roundedProjectilePosition, Vector2Int shift) {
                    var coordinates = roundedProjectilePosition + shift;
                    return !isOutOfMapBounds(coordinates)
                        && gameState.gameTable[coordinates.x, coordinates.y] != CellColor.NONE;
                }

                bool stoppedByMapBounds(Vector2Int destinationCoordinates)
                    => gameSettings.projectilesCollideMapBounds
                    && isOutOfVerticalMapBounds(destinationCoordinates.y);
            }

            bool backPressureProjectile(Projectile projectile, out Vector2Int landPosition) {
                var initProjPos = projectile.position.toVector2Int();
                landPosition = initProjPos;
                Vector2Int newPos = initProjPos - projectile.direction;
                while (gameState.gameTable[newPos] is not CellColor.NONE) {
                    if (isOutOfMapBounds(newPos)) {
                        return false;
                    }
                    newPos -= projectile.direction;
                }
                landPosition = newPos;
                return true;
            }
        }

        /// <summary>
        /// Checks position of player's tetramino. Destroys walls if it should be destroyed.
        /// </summary>
        void checkWallsAroundPlayerTetramino() {
            if (gameState.gamePhase is not GamePhase.Started) { return; }
            var playerPos = gameState.playerTetromino.Position.x;
            for (int locX = 0; locX < gameState.playerTetromino.Size.x; locX++) {
                sendWallToDestructionIfItsFilled(playerPos + locX, -Vector2Int.one);
            }
        }

        void handleGameOver() {
            if (gameState.gamePhase is not GamePhase.GameOver) {
                // Transforming player tetromino to game table cells
                foreach (var plCell in gameState.playerTetromino) {
                    if (gameState.gameTable[plCell] is CellColor.NONE) {
                        gameState.gameTable[plCell] = gameState.playerTetromino.Color;
                        _newCellStream.OnNext(new Cell(plCell, gameState.playerTetromino.Color));
                    }
                }

                SetPhase(GamePhase.GameOver);
            }
        }

        private void handleNewCell(Vector2Int newCellCoordinates) {
            // check that new cell does not collide with player's tetromino
            if (gameState.gamePhase is not GamePhase.GameOver
                && gameState.playerTetromino.Contains(newCellCoordinates)
            ) {
                // Boom
                handleGameOver();
                return;
            }
            // check for full-height wall completeness firstly
            if (sendWallToDestructionIfItsFilled(newCellCoordinates.x, newCellCoordinates)) {
                // The wall was eliminated. Nothing to do any more
                return;
            }
            if (gameState.gamePhase is GamePhase.GameOver) {
                _newCellStream.OnNext(new Cell(newCellCoordinates, gameState.gameTable[newCellCoordinates]));
                _frozenProjectilesStream.OnNext(newCellCoordinates);
                return;
            }
            uint matchedCellsCount = addCellOfProperColorIfNoMatch(
                newCellCoordinates,
                cellPatterns,
                (ColorTable table, CellColor color, Vector2Int neighbourCell)
                    => calculateColorScore(table, color, neighbourCell),
                gameSettings.frozenProjectileColor,
                matchedCellsBuffer
            );
            if (matchedCellsCount > 0) {
                for (int i = 0; i < matchedCellsCount; i++) {
                    eliminateCell(matchedCellsBuffer[i]);
                }
            } else {
                _newCellStream.OnNext(new Cell(newCellCoordinates, gameState.gameTable[newCellCoordinates]));
                _frozenProjectilesStream.OnNext(newCellCoordinates);
            }
        }

        private uint addCellOfProperColorIfNoMatch(
            Vector2Int cellPos,
            ICellPatterns cellPatterns,
            Func<ColorTable, CellColor, Vector2Int, uint> calculateColorScore,
            CellColor defaultColor,
            Vector2Int[] matchedCellsBuffer
        ) {
            uint neighbourCellsCount = 0;
            foreach (Vector2Int dir in Direction.FOUR_DIRECTIONS) {
                if (!isOutOfMapBounds(cellPos + dir)) {
                    CellColor col = gameState.gameTable[cellPos + dir];
                    if (col != CellColor.NONE) {
                        neighbourCellsArray[neighbourCellsCount++] = new Cell(cellPos + dir, col);
                    }
                }
            }

            uint matched = gameState.gameTable.FindPattern(
                cellPatterns, cellPos, matchedCellsBuffer, neighbourCellsArray, neighbourCellsCount
            );
            if (matched > 0) {
                // if there was a pattern match - going out to explode cells
                return matched;
            }

            // No pattern match, making decision what color should new cell have
            if (neighbourCellsArray.Length == 0) {
                gameState.gameTable[cellPos] = defaultColor;
                return 0;
            }
            uint maxScore = 0;
            CellColor colorWinner = defaultColor;
            foreach (CellColor color in Cells.ALL_CELL_TYPES) {
                if (color is CellColor.NONE) {
                    continue;
                }
                int sameColorCounter = 0;
                Cell singleNeighbour = default;
                for (int i = 0; i < neighbourCellsCount; i++) {
                    var neighbour = neighbourCellsArray[i];
                    if (color == neighbour.Color) {
                        sameColorCounter++;
                        singleNeighbour = neighbour;
                    }
                }

                if (sameColorCounter > 1) {
                    // connect two samecoloured neighbour cells with the thitd andgo back
                    gameState.gameTable[cellPos] = color;
                    return 0;
                } else if (sameColorCounter == 1) {
                    // more than one cell


                    uint score = calculateColorScore(gameState.gameTable, color, singleNeighbour.Position);
                    if (score > maxScore) {
                        maxScore = score;
                        colorWinner = color;
                    }
                }
            }

            gameState.gameTable[cellPos] = colorWinner;
            return 0;
        }

        private uint calculateColorScore(ColorTable table, CellColor color, Vector2Int neighbourCell) {
            // let's paint thenew cell with colour of the most bigger neighbour region.
            for (int x = 0; x < table.size.x; x++) {
                for (int y = 0; y < table.size.y; y++) {
                    islandsBuffer[x, y] = false;
                }
            }

            return CountConnectedCells(table, color, neighbourCell, islandsBuffer);
        }

        public static uint CountConnectedCells(
            ColorTable table,
            CellColor color,
            Vector2Int startPointLocal,
            bool[,] islandBuffer
        ) {
            if (table.size == Vector2Int.zero) {
                return 0;
            }
            // Using Flood fill algorithm for painting island during counting it's cells
            return paintConnectedCells(table, color, islandBuffer, startPointLocal);
        }

        private static uint paintConnectedCells(
            ColorTable table,
            CellColor color,
            bool[,] canvas,
            Vector2Int startingPoint
        ) {
            // Using Flood fill algorithm
            // 1. If node is not Inside return.
            if (table.IsOutOfMapBounds(startingPoint)) {
                return 0;
            }
            // 2. Is already painted?
            if (canvas[startingPoint.x, startingPoint.y]) {
                // Already painted -> Return
                return 0;
            }

            // 3. Painting
            uint paintedCount = 0;
            var shouldBePainted = table[startingPoint] == color;
            if (shouldBePainted) {
                canvas[startingPoint.x, startingPoint.y] = true;
                paintedCount++;

                // 4. Recursive flooding in all four directions away from current cell
                foreach (var dir in Direction.FOUR_DIRECTIONS) {
                    paintedCount += paintConnectedCells(table, color, canvas, startingPoint + dir);
                }
            }

            return paintedCount;
        }

        private bool sendWallToDestructionIfItsFilled(int wallX, Vector2Int withProjectile) {
            var plCells = gameState.playerTetromino.GetVerticalCells(wallX);
            // Compare buffered wall and new wall
            bool shouldBeEliminated = true;
            for (int y = 0; y < gameState.gameTable.size.y; ++y) {
                if (
                    v2i(wallX, y) != withProjectile
                    && gameState.gameTable[wallX, y] is CellColor.NONE
                    && !plCells.Contains(v2i(wallX, y))
                ) {
                    shouldBeEliminated = false;
                    break;
                }
            }
            if (shouldBeEliminated) {
                _wallEliminationStream.OnNext((wallX, withProjectile));
            }
            return shouldBeEliminated;
        }

        private void eliminateWall(int wallX, Vector2Int withProjectile) {
            var plCells = gameState.playerTetromino.GetVerticalCells(wallX);
            // blocks destruction
            for (int y = 0; y < gameState.gameTable.size.y; y++) {
                if (
                    !plCells.Contains(new Vector2Int(wallX, y))
                    || v2i(wallX, y) == withProjectile
                ) {
                    eliminateCell(v2i(wallX, y));
                }
            }
        }

        private void eliminateCell(Vector2Int pos) {
            var oldColor = gameState.gameTable[pos.x, pos.y];
            oldColor = oldColor != CellColor.NONE ? oldColor : gameSettings.frozenProjectileColor;
            gameState.gameTable.RemoveCell(pos);
            _eliminatedBricksStream.OnNext(new Cell(pos, oldColor));
            gameState.scores++;
            _scoreStream.OnNext(gameState.scores);
        }

        private Vector2Int calculateNewPlayerPosition(Vector2Int moveDir) {
            var newPlayerPos = gameState.playerTetromino.Position + moveDir;
            newPlayerPos = new Vector2Int(
                Mathf.Clamp(newPlayerPos.x, 0, gameState.gameTable.size.x - gameState.playerTetromino.Size.x),
                Mathf.Clamp(newPlayerPos.y, 0, gameState.gameTable.size.y - gameState.playerTetromino.Size.y));
            return newPlayerPos;
        }

        private static Vector2Int getMoveDirection(MovementInput input) {
            var hor = input.horizontal is HorizontalInput.Right
                ? 1 :
                input.horizontal is HorizontalInput.Left ? -1 : 0;
            var ver = input.vertical is VerticalInput.Up
                ? 1
                : input.vertical is VerticalInput.Down ? -1 : 0;
            return v2i(hor, ver);
        }

        private bool checkPlayerTetrominoCollisions(PlayerTetromino tetromino, Vector2Int shift = default) {
            foreach (var cell in tetromino) {
                if (!isOutOfMapBounds(cell + shift) && gameState.gameTable[cell + shift] != CellColor.NONE) {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfHorizontalMapBounds(float x) => gameState.gameTable.IsOutOfHorizontalMapBounds(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfVerticalMapBounds(float y) => gameState.gameTable.IsOutOfVerticalMapBounds(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfMapBounds(Vector2 position)
            => gameState.gameTable.IsOutOfMapBounds(position);

        class GameInputStreams : IGameInputBus {
            readonly ISubject<MovementInput> _playerMovementStream = new Subject<MovementInput>();
            ISubject<MovementInput> IGameInputBus.PlayerMovementStream => _playerMovementStream;

            readonly ISubject<Unit> _playerShotStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.PlayerShotStream => _playerShotStream;

            readonly ISubject<bool> _playerRotateStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.PlayerRotateStream => _playerRotateStream;

            readonly ISubject<Unit> _gameStartStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.GameStartStream => _gameStartStream;

            readonly ISubject<bool> _gamePauseResumeStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.GamePauseResumeStream => _gamePauseResumeStream;

            public GameInputStreams() { }
        }

        class TimeEventBus : IGameTimeBus {
            ISubject<float> _frameUpdateStream = new Subject<float>();
            public ISubject<float> FrameUpdatePublisher => _frameUpdateStream;
        }
    }

    internal struct WallSpawnEnumerable : IEnumerable<CellColor[]> {

        private ICellGenerator cellGenerator;
        private CellColor[] wallSpawnBuffer;
        private int wallsCount;

        public WallSpawnEnumerable(ICellGenerator cellGenerator, CellColor[] wallSpawnBuffer, int wallsCount) {
            this.cellGenerator = cellGenerator;
            this.wallSpawnBuffer = wallSpawnBuffer;
            this.wallsCount = wallsCount;
        }


        public WallSpawnEnumerator GetEnumerator() {
            return new WallSpawnEnumerator(cellGenerator, wallSpawnBuffer, wallsCount);
        }

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator<CellColor[]> IEnumerable<CellColor[]>.GetEnumerator() => this.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
    }

    internal struct WallSpawnEnumerator : IEnumerator<CellColor[]> {

        private ICellGenerator cellGenerator;
        private CellColor[] wallSpawnBuffer;
        private int wallsCount;

        bool movedNext;
        bool hasNext;
        int currentWallInd;

        public WallSpawnEnumerator(ICellGenerator cellGenerator, CellColor[] wallSpawnBuffer, int wallsCount) {
            this.cellGenerator = cellGenerator;
            this.wallSpawnBuffer = wallSpawnBuffer;
            this.wallsCount = wallsCount;
            movedNext = false;
            hasNext = false;
            currentWallInd = 0;
        }

        public CellColor[] Current =>
            !movedNext || !hasNext
            ? throw new InvalidOperationException("No more elements to iterate!")
            : wallSpawnBuffer;

        object IEnumerator.Current => Current;


        public bool MoveNext() {
            movedNext = true;
            if (wallsCount != 0 && currentWallInd < wallsCount) {
                cellGenerator.GenerateCells(wallSpawnBuffer);
                currentWallInd++;
                hasNext = true;
            } else {
                hasNext = false;
            }
            return hasNext;
        }

        public void Reset() {
            movedNext = false;
            hasNext = false;
            currentWallInd = 0;
        }
        public void Dispose() { }
    }

}