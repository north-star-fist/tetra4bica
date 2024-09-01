using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sergei.Safonov.Utility;
using UniRx;
using UnityEngine;
using Zenject;
using static Sergei.Safonov.Utility.VectorExt;
using static Tetra4bica.Input.PlayerInput;
using static Tetra4bica.Input.PlayerInput.MovementInput;

namespace Tetra4bica.Core
{

    /// <summary>
    /// Class that gets game input streams and gives game change streams.
    /// </summary>
    [ZenjectAllowDuringValidation]
    public class GameLogic : IGameEvents
    {
        public GameState GameState => gameState;

        private GameSettings gameSettings;
        private ICellPatterns cellPatterns;
        private ICellGenerator cellGenerator;
        private GameState gameState;

        #region Game event streams
        private IObservable<float> frameUpdateStream;
        public IObservable<float> FrameUpdateStream => frameUpdateStream;

        IObservable<IEnumerable<CellColor?>> tableScrollStream;
        /// <summary>
        /// Scrolls game table cells left for one column passing new the most right column of the table.
        /// </summary>
        public IObservable<IEnumerable<CellColor?>> TableScrollStream => tableScrollStream;

        private Subject<Vector2Int> gameStartedStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> GameStartedStream => gameStartedStream.AsObservable();

        private Subject<Cell> newCellStream = new Subject<Cell>();
        public IObservable<Cell> NewCellStream => newCellStream.AsObservable();

        private Subject<PlayerTetromino> playerTetrominoStream = new Subject<PlayerTetromino>();
        public IObservable<PlayerTetromino> PlayerTetrominoStream => playerTetrominoStream.AsObservable();

        private Subject<Vector2Int> shotStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> ShotStream => shotStream.AsObservable();

        private Subject<bool> rotationStream = new Subject<bool>();
        public IObservable<bool> RotationStream => rotationStream.AsObservable();

        private Subject<Cell> eliminatedBricksStream = new Subject<Cell>();
        public IObservable<Cell> EliminatedBricksStream => eliminatedBricksStream.AsObservable();

        private Subject<Vector2> projectileCoordinatesStream = new Subject<Vector2>();
        public IObservable<Vector2> ProjectileCoordinatesStream => projectileCoordinatesStream;

        private Subject<Vector2Int> frozenProjectilesStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> FrozenProjectilesStream => frozenProjectilesStream;

        private Subject<uint> scoreStream = new Subject<uint>();
        public IObservable<uint> ScoreStream => scoreStream;

        private Subject<GamePhase> gamePhaseStream = new Subject<GamePhase>();
        public IObservable<GamePhase> GamePhaseStream => gamePhaseStream;

        #endregion

        IGameInputBus playerInputBus = new GameInputStreams();
        IGameTimeBus timeEventsBus = new TimeEventBus();

        // Private inner streams
        /// <summary> Frozen projectiles inner stream. </summary>
        Subject<Vector2Int> frozenProjectilesInnerStream = new Subject<Vector2Int>();
        // The stream of events when the whole wall shouldbeeliminated. Need to be combined with frame stream to
        // eliminate wall when it isupdated on UI.
        Subject<(int, Vector2Int)> wallEliminationStream = new Subject<(int, Vector2Int)>();

        // Buffer for holding cells that match some pattern.
        // Assuming there is no cell patterns with more than 16 cells area.
        Vector2Int[] matchedCellsBuffer = new Vector2Int[16];

        // Buffer for new wall cells.
        CellColor?[] wallSpawnBuffer;

        // Buffer array for finding colorislands
        // (used for counting cells in color region for making new cell color decision)
        bool[,] islandsBuffer;

        // Array for tempopary storing neighbour cells during calculations.
        Cell[] neighbourCellsArray = new Cell[Direction.FOUR_DIRECTIONS.Length];

        // Not to record game over before event that bring us here waiting for one frame
        private bool handleGameOverNextFrame;

        public struct GameSettings
        {
            readonly public int MapWidth;
            readonly public int MapHeight;
            readonly public float ScrollTimeStep;
            readonly public Vector2Int PlayerStartPosition;
            readonly public CellColor PlayerColor;
            readonly public CellColor FrozenProjectileColor;
            readonly public float ProjectileSpeed;
            readonly public bool LateralBricksStopProjectiles;
            readonly public bool ProjectilesCollideMapBounds;

            public GameSettings(
                int mapWidth,
                int mapHeight,
                float scrollTimeStep,
                Vector2Int playerStartPosition,
                CellColor playerColor,
                CellColor frozenProjectileColor,
                float projectileSpeed,
                bool lateralBricksStopProjectiles,
                bool projectilesCollideMapBounds
            )
            {
                this.MapWidth = mapWidth;
                this.MapHeight = mapHeight;
                this.ScrollTimeStep = scrollTimeStep;
                this.PlayerStartPosition = playerStartPosition;
                this.PlayerColor = playerColor;
                this.FrozenProjectileColor = frozenProjectileColor;
                this.ProjectileSpeed = projectileSpeed;
                this.LateralBricksStopProjectiles = lateralBricksStopProjectiles;
                this.ProjectilesCollideMapBounds = projectilesCollideMapBounds;
            }
        }

        [Inject]
        public GameLogic(
            GameSettings gameSettings,
            IGameInputEventProvider inputEventProvider,
            ICellPatterns tetraminoPatterns,
            ICellGenerator cellGenerator
        )
        {
            this.gameSettings = gameSettings;
            this.cellPatterns = tetraminoPatterns;
            this.cellGenerator = cellGenerator;
            wallSpawnBuffer = new CellColor?[gameSettings.MapHeight];

            frameUpdateStream = timeEventsBus.FrameUpdatePublisher;

            IObservable<int> scrollStepStream = frameUpdateStream
                .Where(_ => gameState.GamePhase is not GamePhase.Paused and not GamePhase.NotStarted)
                .Scan((acc, delta) => acc + delta)
                .Select(time => (int)(time / gameSettings.ScrollTimeStep))
                .DistinctUntilChanged()
                .Scan((0, 0), (oldStepsAndDelta, newSteps)
                    =>
                { return (newSteps, newSteps - oldStepsAndDelta.Item1); })
                .SelectMany(stepsWithDelta => Enumerable.Range(stepsWithDelta.Item1 - stepsWithDelta.Item2, stepsWithDelta.Item2));
            // Making TableScrollStream connectable Observable not to repeat side effects for each subscriber
            // (as new column generation).
            tableScrollStream = scrollStepStream
                .Do(_ => this.cellGenerator.GenerateCells(wallSpawnBuffer))
                .Select(steps => wallSpawnBuffer).Share();

            inputEventProvider.GetInputStream().Subscribe(e => e.Apply(timeEventsBus, playerInputBus));

            frameUpdateStream.Where(_ => gameState.GamePhase is not GamePhase.Paused or GamePhase.NotStarted)
                .Subscribe(deltaTime =>
                {
                    handleProjectiles(deltaTime);
                });
            frameUpdateStream.Subscribe(_ =>
            {
                if (handleGameOverNextFrame)
                {
                    handleGameOverNextFrame = false;
                    handleGameOver();
                }
            });


            TableScrollStream.Subscribe(newWall =>
            {
                scrollBricks(newWall);
                checkWallsAroundPlayerTetramino();
            });

            // Running elimination of wall on the following frame update because when table scrolls onto player tetramino
            // View should be updated first, and then exploded
            wallEliminationStream.SelectMany(
                wData => frameUpdateStream.First().Select(_ => wData))
                .Subscribe(tpl => eliminateWall(tpl.Item1, tpl.Item2));

            playerInputBus.PlayerMovementStream.Subscribe(movePlayer);
            playerInputBus.PlayerRotateStream.Subscribe(handleRotation);
            playerInputBus.PlayerShotStream.Subscribe(_ => handleShot());

            frozenProjectilesInnerStream.Subscribe(handleNewCell);
            playerInputBus.GameStartStream
            .Subscribe(tableSize => SetPhase(GamePhase.Started));
            playerInputBus.GamePauseResumeStream.Subscribe(
                paused => SetPhase(paused ? GamePhase.Paused : GamePhase.Started)
            );


            resetGameState();
        }

        /// <summary> Sets new game phase. </summary>
        /// <returns>old phase</returns>
        GamePhase SetPhase(GamePhase gamePhase)
        {
            var oldPhase = gameState.GamePhase;
            if (gamePhase == GamePhase.Started)
            {
                if (gameState.GamePhase is GamePhase.NotStarted or GamePhase.GameOver)
                {
                    StartNewGame();
                }
                else if (gameState.GamePhase is GamePhase.Paused)
                {
                    // unpause
                }
            }
            gameState.SetGamePhase(gamePhase);
            gamePhaseStream.OnNext(gamePhase);
            //Debug.Log($"Switching {oldPhase} to {gamePhase}");
            return oldPhase;
        }

        void StartNewGame()
        {
            resetGameState();
            islandsBuffer = new bool[gameSettings.MapWidth, gameSettings.MapHeight];
            gameStartedStream.OnNext(new Vector2Int(gameSettings.MapWidth, gameSettings.MapHeight));
            playerTetrominoStream.OnNext(gameState.PlayerTetromino);
        }

        void scrollBricks(IEnumerable<CellColor?> newWall)
        {
            if (gameState.GamePhase is not GamePhase.GameOver)
            {
                if (!checkPlayerTetrominoCollisions(gameState.PlayerTetromino, Vector2Int.right)
                    || isNewColumnCollidingPlayer(newWall))
                {
                    handleGameOver();
                }
            }
            gameState.GameTable.ScrollLeft(newWall);

            bool isNewColumnCollidingPlayer(IEnumerable<CellColor?> newWall)
            {
                int y = 0;
                foreach (CellColor? cellColor in newWall)
                {
                    if (cellColor.HasValue
                        && gameState.PlayerTetromino.Contains(v2i(gameState.GameTable.Size.x - 1, y))
                    )
                    {
                        return true;
                    }
                    y++;
                }
                return false;
            }
        }

        private void resetGameState()
        {
            gameState = new GameState(
                new PlayerTetromino(gameSettings.PlayerStartPosition, gameSettings.PlayerColor),
                // Doubled quantity of table cells should be enough for keeping all particles on the screen
                new Projectile[gameSettings.MapWidth * gameSettings.MapHeight * 2],
                GamePhase.NotStarted,
                new ColorTable(v2i(gameSettings.MapWidth, gameSettings.MapHeight))
            );
        }

        void movePlayer(MovementInput input)
        {
            if (gameState.GamePhase is not GamePhase.Started)
            { return; }

            Vector2Int dir = getMoveDirection(input);
            if (!checkPlayerTetrominoCollisions(gameState.PlayerTetromino, dir))
            {
                handleGameOverNextFrame = true;
                return;
            }
            gameState.SetPlayerTetromino(gameState.PlayerTetromino.WithPosition(calculateNewPlayerPosition(dir)));
            playerTetrominoStream.OnNext(gameState.PlayerTetromino);
            checkWallsAroundPlayerTetramino();
        }

        private void handleRotation(bool clockwise)
        {
            if (gameState.GamePhase is not GamePhase.Started)
            { return; }

            PlayerTetromino rotatedTetramino = gameState.PlayerTetromino.Rotate(clockwise);
            rotatedTetramino = fixPositionAfterRotation(rotatedTetramino);
            if (!checkPlayerTetrominoCollisions(rotatedTetramino))
            {
                handleGameOverNextFrame = true;
                return;
            }
            gameState.SetPlayerTetromino(rotatedTetramino);
            playerTetrominoStream.OnNext(gameState.PlayerTetromino);
            rotationStream.OnNext(clockwise);

            // Arranges tetramino position if it exeeded map bounds after rotation
            PlayerTetromino fixPositionAfterRotation(PlayerTetromino tetramino)
            {
                var size = tetramino.Size;
                var position = tetramino.Position;
                Vector2Int fixedPosition = new Vector2Int(
                    Mathf.Clamp(position.x, 0, gameState.GameTable.Size.x - size.x),
                    Mathf.Clamp(position.y, 0, gameState.GameTable.Size.y - size.y)
                );
                return tetramino.WithPosition(fixedPosition);
            }
        }

        private void handleShot()
        {
            if (gameState.GamePhase is not GamePhase.Started)
            { return; }

            shotStream.OnNext(gameState.PlayerTetromino.Direction);

            Vector2Int projectileStartPosition = gameState.PlayerTetromino.Position
                + gameState.PlayerTetromino.Muzzle;

            if (
                !isOutOfMapBounds(projectileStartPosition)
                && !gameState.GameTable[projectileStartPosition].HasValue
            )
            {
                gameState.IncNextProjectileInd();
                gameState.Projectiles[gameState.NextProjectileInd] =
                    new Projectile(projectileStartPosition, gameState.PlayerTetromino.Direction);
                if (gameState.NextProjectileInd >= gameState.Projectiles.Length)
                {
                    gameState.ResetNextProjectileInd();
                }
            }
        }

        void handleProjectiles(float deltaTime)
        {
            if (gameState.GamePhase is GamePhase.Paused)
            { return; }

            for (int i = 0; i < gameState.Projectiles.Length; i++)
            {
                Projectile projectile = gameState.Projectiles[i];
                if (projectile.Active)
                {
                    bool flewAway = gameSettings.ProjectilesCollideMapBounds
                        ? isOutOfHorizontalMapBounds(projectile.Position.x)
                        : isOutOfMapBounds(projectile.Position);
                    if (!flewAway)
                    {
                        if (shouldBeFrozen(projectile, deltaTime, out Vector2Int landPosition))
                        {
                            frozenProjectilesInnerStream.OnNext(landPosition);
                            projectile.Active = false;
                        }
                    }
                    else
                    {
                        projectile.Active = false;
                    }
                    if (projectile.Active)
                    { //still
                        projectile.Position = projectile.Position
                            + projectile.Direction.toVector2() * gameSettings.ProjectileSpeed * deltaTime;
                        projectileCoordinatesStream.OnNext(projectile.Position);
                    }
                    // Writing updated projectile back to the array
                    gameState.Projectiles[i] = projectile;
                }
            }
        }

        private bool shouldBeFrozen(Projectile projectile, float deltaTime, out Vector2Int landPosition)
        {
            landPosition = default;
            // checking integer position
            Vector2Int intPos = projectile.Position.toVector2Int();
            if ((gameSettings.ProjectilesCollideMapBounds && isOutOfHorizontalMapBounds(intPos.x))
                || isOutOfMapBounds(intPos)
            )
            {
                return false;
            }

            // Checking each cell passed by projectile to decide does it collide with anything
            // If The prijectile is already above occupied cell then backpress the projectile back to freeze it in 
            // free space. During backpressing we should check does the projectile collides with the player tetromino
            // itself. In this case player tetromino goes down (game over)
            if (gameState.GameTable[projectile.Position.toVector2Int()].HasValue)
            {
                // projectile cell is occupied already. Projectile should be backpressured
                return backPressureProjectile(projectile, out landPosition);
            }
            var farthestPosition = projectile.Position + projectile.Direction.toVector2() * gameSettings.ProjectileSpeed * deltaTime;
            Vector2Int posDelta = (farthestPosition - projectile.Position.toVector2Int()).toVector2Int();
            for (int passedCells = 0; (passedCells * projectile.Direction).sqrMagnitude <= posDelta.sqrMagnitude; passedCells++)
            {
                Vector2Int newPosition =
                    (projectile.Position + (passedCells * projectile.Direction)).toVector2Int();
                if (shouldBeFrozen(projectile.Direction, newPosition, out landPosition))
                {
                    return true;
                }
            }
            return false;

            bool shouldBeFrozen(Vector2Int direction, Vector2Int newPosition, out Vector2Int landPosition)
            {
                landPosition = newPosition;
                // Check rubberish neighbours
                if (gameSettings.LateralBricksStopProjectiles &&
                    (checkNeighbourCell(newPosition, Vector2Int.left)
                    || checkNeighbourCell(newPosition, Vector2Int.right)
                    || checkNeighbourCell(newPosition, Vector2Int.down)
                    || checkNeighbourCell(newPosition, Vector2Int.up))
                )
                {
                    return true;
                }
                // Check cell in front of the projectile
                if (checkNeighbourCell(newPosition, direction))
                {
                    return true;
                }
                //Check borders
                return stoppedByMapBounds(newPosition + direction);

                bool checkNeighbourCell(Vector2Int roundedProjectilePosition, Vector2Int shift)
                {
                    var coordinates = roundedProjectilePosition + shift;
                    return !isOutOfMapBounds(coordinates)
                        && gameState.GameTable[coordinates.x, coordinates.y].HasValue;
                }

                bool stoppedByMapBounds(Vector2Int destinationCoordinates)
                    => gameSettings.ProjectilesCollideMapBounds
                    && isOutOfVerticalMapBounds(destinationCoordinates.y);
            }

            bool backPressureProjectile(Projectile projectile, out Vector2Int landPosition)
            {
                var initProjPos = projectile.Position.toVector2Int();
                landPosition = initProjPos;
                Vector2Int newPos = initProjPos - projectile.Direction;
                while (gameState.GameTable[newPos].HasValue)
                {
                    if (isOutOfMapBounds(newPos))
                    {
                        return false;
                    }
                    newPos -= projectile.Direction;
                }
                landPosition = newPos;
                return true;
            }
        }

        /// <summary>
        /// Checks position of player's tetramino. Destroys walls if it should be destroyed.
        /// </summary>
        void checkWallsAroundPlayerTetramino()
        {
            if (gameState.GamePhase is not GamePhase.Started)
            { return; }
            var playerPos = gameState.PlayerTetromino.Position.x;
            for (int locX = 0; locX < gameState.PlayerTetromino.Size.x; locX++)
            {
                sendWallToDestructionIfItsFilled(playerPos + locX, -Vector2Int.one);
            }
        }

        void handleGameOver()
        {
            if (gameState.GamePhase is not GamePhase.GameOver)
            {
                // Transforming player tetromino to game table cells
                foreach (var plCell in gameState.PlayerTetromino)
                {
                    if (!gameState.GameTable[plCell].HasValue)
                    {
                        gameState.GameTable[plCell] = gameState.PlayerTetromino.Color;
                        newCellStream.OnNext(new Cell(plCell, gameState.PlayerTetromino.Color));
                    }
                }

                SetPhase(GamePhase.GameOver);
            }
        }

        private void handleNewCell(Vector2Int newCellCoordinates)
        {
            // check that new cell does not collide with player's tetromino
            if (gameState.GamePhase is not GamePhase.GameOver
                && gameState.PlayerTetromino.Contains(newCellCoordinates)
            )
            {
                // Boom
                handleGameOver();
                return;
            }
            // check for full-height wall completeness firstly
            if (sendWallToDestructionIfItsFilled(newCellCoordinates.x, newCellCoordinates))
            {
                // The wall was eliminated. Nothing to do any more
                return;
            }
            if (gameState.GamePhase is GamePhase.GameOver)
            {
                // Freezing projectiles on game over. no matter what color it should be
                newCellStream.OnNext(new Cell(newCellCoordinates, gameSettings.FrozenProjectileColor));
                frozenProjectilesStream.OnNext(newCellCoordinates);
                return;
            }
            uint matchedCellsCount = addCellOfProperColorIfNoMatch(
                newCellCoordinates,
                cellPatterns,
                (ColorTable table, CellColor color, Vector2Int neighbourCell)
                    => calculateColorScore(table, color, neighbourCell),
                gameSettings.FrozenProjectileColor,
                matchedCellsBuffer,
                out CellColor? patternColor
            );
            if (matchedCellsCount > 0)
            {
                for (int i = 0; i < matchedCellsCount; i++)
                {
                    eliminateCell(new Cell(matchedCellsBuffer[i], patternColor.Value));
                }
            }
            else
            {
                // We added the cell into the table above, so it must have some color
                var newCellColor = gameState.GameTable[newCellCoordinates].Value;
                newCellStream.OnNext(new Cell(newCellCoordinates, newCellColor));
                frozenProjectilesStream.OnNext(newCellCoordinates);
            }
        }

        private uint addCellOfProperColorIfNoMatch(
            Vector2Int cellPos,
            ICellPatterns cellPatterns,
            Func<ColorTable, CellColor, Vector2Int, uint> calculateColorScore,
            CellColor defaultColor,
            Vector2Int[] matchedCellsBuffer,
            out CellColor? patternColor
        )
        {
            uint neighbourCellsCount = 0;
            foreach (Vector2Int dir in Direction.FOUR_DIRECTIONS)
            {
                if (!isOutOfMapBounds(cellPos + dir))
                {
                    CellColor? col = gameState.GameTable[cellPos + dir];
                    if (col.HasValue)
                    {
                        neighbourCellsArray[neighbourCellsCount++] = new Cell(cellPos + dir, col.Value);
                    }
                }
            }

            uint matched = gameState.GameTable.FindPattern(
                cellPatterns, cellPos, matchedCellsBuffer, neighbourCellsArray, neighbourCellsCount, out patternColor
            );
            if (matched > 0)
            {
                // if there was a pattern match - going out to explode cells
                return matched;
            }

            // No pattern match, making decision what color should new cell have
            if (neighbourCellsArray.Length == 0)
            {
                gameState.GameTable[cellPos] = defaultColor;
                return 0;
            }
            uint maxScore = 0;
            CellColor colorWinner = defaultColor;
            foreach (CellColor color in Cells.ALL_CELL_TYPES)
            {
                int sameColorCounter = 0;
                Cell singleNeighbour = default;
                for (int i = 0; i < neighbourCellsCount; i++)
                {
                    var neighbour = neighbourCellsArray[i];
                    if (color == neighbour.Color)
                    {
                        sameColorCounter++;
                        singleNeighbour = neighbour;
                    }
                }

                if (sameColorCounter > 1)
                {
                    // connect two samecoloured neighbour cells with the thitd andgo back
                    gameState.GameTable[cellPos] = color;
                    return 0;
                }
                else if (sameColorCounter == 1)
                {
                    // more than one cell


                    uint score = calculateColorScore(gameState.GameTable, color, singleNeighbour.Position);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        colorWinner = color;
                    }
                }
            }

            gameState.GameTable[cellPos] = colorWinner;
            return 0;
        }

        private uint calculateColorScore(ColorTable table, CellColor color, Vector2Int neighbourCell)
        {
            // let's paint thenew cell with colour of the most bigger neighbour region.
            for (int x = 0; x < table.Size.x; x++)
            {
                for (int y = 0; y < table.Size.y; y++)
                {
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
        )
        {
            if (table.Size == Vector2Int.zero)
            {
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
        )
        {
            // Using Flood fill algorithm
            // 1. If node is not Inside return.
            if (table.IsOutOfMapBounds(startingPoint))
            {
                return 0;
            }
            // 2. Is already painted?
            if (canvas[startingPoint.x, startingPoint.y])
            {
                // Already painted -> Return
                return 0;
            }

            // 3. Painting
            uint paintedCount = 0;
            var shouldBePainted = table[startingPoint] == color;
            if (shouldBePainted)
            {
                canvas[startingPoint.x, startingPoint.y] = true;
                paintedCount++;

                // 4. Recursive flooding in all four directions away from current cell
                foreach (var dir in Direction.FOUR_DIRECTIONS)
                {
                    paintedCount += paintConnectedCells(table, color, canvas, startingPoint + dir);
                }
            }

            return paintedCount;
        }

        private bool sendWallToDestructionIfItsFilled(int wallX, Vector2Int withProjectile)
        {
            var plCells = gameState.PlayerTetromino.GetVerticalCells(wallX);
            // Compare buffered wall and new wall
            bool shouldBeEliminated = true;
            for (int y = 0; y < gameState.GameTable.Size.y; ++y)
            {
                if (
                    v2i(wallX, y) != withProjectile
                    && !gameState.GameTable[wallX, y].HasValue
                    && !plCells.Contains(v2i(wallX, y))
                )
                {
                    shouldBeEliminated = false;
                    break;
                }
            }
            if (shouldBeEliminated)
            {
                wallEliminationStream.OnNext((wallX, withProjectile));
            }
            return shouldBeEliminated;
        }

        private void eliminateWall(int wallX, Vector2Int withProjectile)
        {
            var plCells = gameState.PlayerTetromino.GetVerticalCells(wallX);
            // blocks destruction
            for (int y = 0; y < gameState.GameTable.Size.y; y++)
            {
                var pos = v2i(wallX, y);
                if (!plCells.Contains(new Vector2Int(wallX, y)) || pos == withProjectile)
                {
                    CellColor? cellColor = pos != withProjectile
                        ? gameState.GameTable[pos]
                        : gameSettings.FrozenProjectileColor;
                    if (cellColor.HasValue)
                    {
                        eliminateCell(new Cell(pos, cellColor.Value));
                    }
                    else
                    {
                        Debug.LogError($"Trying to eliminate EMPTY cell {pos}!");
                    }
                }
            }
        }

        private void eliminateCell(Cell cell)
        {
            gameState.GameTable.RemoveCell(cell.Position);
            eliminatedBricksStream.OnNext(cell);
            gameState.IncScore();
            scoreStream.OnNext(gameState.Scores);
        }

        private Vector2Int calculateNewPlayerPosition(Vector2Int moveDir)
        {
            var newPlayerPos = gameState.PlayerTetromino.Position + moveDir;
            newPlayerPos = new Vector2Int(
                Mathf.Clamp(newPlayerPos.x, 0, gameState.GameTable.Size.x - gameState.PlayerTetromino.Size.x),
                Mathf.Clamp(newPlayerPos.y, 0, gameState.GameTable.Size.y - gameState.PlayerTetromino.Size.y));
            return newPlayerPos;
        }

        private static Vector2Int getMoveDirection(MovementInput input)
        {
            var hor = input.Horizontal is HorizontalInput.Right
                ? 1 :
                input.Horizontal is HorizontalInput.Left ? -1 : 0;
            var ver = input.Vertical is VerticalInput.Up
                ? 1
                : input.Vertical is VerticalInput.Down ? -1 : 0;
            return v2i(hor, ver);
        }

        private bool checkPlayerTetrominoCollisions(PlayerTetromino tetromino, Vector2Int shift = default)
        {
            foreach (var cell in tetromino)
            {
                if (!isOutOfMapBounds(cell + shift) && gameState.GameTable[cell + shift].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfHorizontalMapBounds(float x) => gameState.GameTable.IsOutOfHorizontalMapBounds(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfVerticalMapBounds(float y) => gameState.GameTable.IsOutOfVerticalMapBounds(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfMapBounds(Vector2 position)
            => gameState.GameTable.IsOutOfMapBounds(position);

        class GameInputStreams : IGameInputBus
        {
            readonly ISubject<MovementInput> playerMovementStream = new Subject<MovementInput>();
            ISubject<MovementInput> IGameInputBus.PlayerMovementStream => playerMovementStream;

            readonly ISubject<Unit> playerShotStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.PlayerShotStream => playerShotStream;

            readonly ISubject<bool> playerRotateStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.PlayerRotateStream => playerRotateStream;

            readonly ISubject<Unit> gameStartStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.GameStartStream => gameStartStream;

            readonly ISubject<bool> gamePauseResumeStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.GamePauseResumeStream => gamePauseResumeStream;

            public GameInputStreams() { }
        }

        class TimeEventBus : IGameTimeBus
        {
            readonly ISubject<float> frameUpdateStream = new Subject<float>();
            public ISubject<float> FrameUpdatePublisher => frameUpdateStream;
        }
    }

    internal struct WallSpawnEnumerable : IEnumerable<CellColor?[]>
    {

        private ICellGenerator cellGenerator;
        private CellColor?[] wallSpawnBuffer;
        private int wallsCount;

        public WallSpawnEnumerable(ICellGenerator cellGenerator, CellColor?[] wallSpawnBuffer, int wallsCount)
        {
            this.cellGenerator = cellGenerator;
            this.wallSpawnBuffer = wallSpawnBuffer;
            this.wallsCount = wallsCount;
        }


        public WallSpawnEnumerator GetEnumerator()
        {
            return new WallSpawnEnumerator(cellGenerator, wallSpawnBuffer, wallsCount);
        }

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator<CellColor?[]> IEnumerable<CellColor?[]>.GetEnumerator() => this.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
    }

    internal struct WallSpawnEnumerator : IEnumerator<CellColor?[]>
    {

        private ICellGenerator cellGenerator;
        private CellColor?[] wallSpawnBuffer;
        private int wallsCount;

        bool movedNext;
        bool hasNext;
        int currentWallInd;

        public WallSpawnEnumerator(ICellGenerator cellGenerator, CellColor?[] wallSpawnBuffer, int wallsCount)
        {
            this.cellGenerator = cellGenerator;
            this.wallSpawnBuffer = wallSpawnBuffer;
            this.wallsCount = wallsCount;
            movedNext = false;
            hasNext = false;
            currentWallInd = 0;
        }

        public CellColor?[] Current =>
            !movedNext || !hasNext
            ? throw new InvalidOperationException("No more elements to iterate!")
            : wallSpawnBuffer;

        object IEnumerator.Current => Current;


        public bool MoveNext()
        {
            movedNext = true;
            if (wallsCount != 0 && currentWallInd < wallsCount)
            {
                cellGenerator.GenerateCells(wallSpawnBuffer);
                currentWallInd++;
                hasNext = true;
            }
            else
            {
                hasNext = false;
            }
            return hasNext;
        }

        public void Reset()
        {
            movedNext = false;
            hasNext = false;
            currentWallInd = 0;
        }
        public void Dispose() { }
    }

}
