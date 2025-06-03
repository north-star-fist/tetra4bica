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
        public GameState GameState => _gameState;

        private GameSettings _gameSettings;
        private readonly ICellPatterns _cellPatterns;
        private readonly ICellGenerator _cellGenerator;
        private GameState _gameState;

        #region Game event streams
        private readonly IObservable<float> _frameUpdateStream;
        public IObservable<float> FrameUpdateStream => _frameUpdateStream;

        private readonly IObservable<IEnumerable<CellColor?>> _tableScrollStream;
        /// <summary>
        /// Scrolls game table cells left for one column passing new the most right column of the table.
        /// </summary>
        public IObservable<IEnumerable<CellColor?>> TableScrollStream => _tableScrollStream;

        private readonly Subject<Vector2Int> _gameStartedStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> GameStartedStream => _gameStartedStream.AsObservable();

        private readonly Subject<Cell> _newCellStream = new Subject<Cell>();
        public IObservable<Cell> NewCellStream => _newCellStream.AsObservable();

        private readonly Subject<PlayerTetromino> _playerTetrominoStream = new Subject<PlayerTetromino>();
        public IObservable<PlayerTetromino> PlayerTetrominoStream => _playerTetrominoStream.AsObservable();

        private readonly Subject<Vector2Int> _shotStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> ShotStream => _shotStream.AsObservable();

        private readonly Subject<bool> _rotationStream = new Subject<bool>();
        public IObservable<bool> RotationStream => _rotationStream.AsObservable();

        private readonly Subject<Cell> _eliminatedBricksStream = new Subject<Cell>();
        public IObservable<Cell> EliminatedBricksStream => _eliminatedBricksStream.AsObservable();

        private readonly Subject<Vector2> _projectileCoordinatesStream = new Subject<Vector2>();
        public IObservable<Vector2> ProjectileCoordinatesStream => _projectileCoordinatesStream;

        private readonly Subject<Vector2Int> _frozenProjectilesStream = new Subject<Vector2Int>();
        public IObservable<Vector2Int> FrozenProjectilesStream => _frozenProjectilesStream;

        private readonly Subject<uint> _scoreStream = new Subject<uint>();
        public IObservable<uint> ScoreStream => _scoreStream;

        private readonly Subject<GamePhase> _gamePhaseStream = new Subject<GamePhase>();
        public IObservable<GamePhase> GamePhaseStream => _gamePhaseStream;

        #endregion

        private readonly IGameInputBus _playerInputBus = new GameInputStreams();
        private readonly IGameTimeBus _timeEventsBus = new TimeEventBus();

        // Private inner streams
        /// <summary> Frozen projectiles inner stream. </summary>
        private readonly Subject<Vector2Int> _frozenProjectilesInnerStream = new Subject<Vector2Int>();
        // The stream of events when the whole wall shouldbeeliminated. Need to be combined with frame stream to
        // eliminate wall when it isupdated on UI.
        private readonly Subject<(int, Vector2Int)> _wallEliminationStream = new Subject<(int, Vector2Int)>();

        // Buffer for holding cells that match some pattern.
        // Assuming there is no cell patterns with more than 16 cells area.
        private readonly Vector2Int[] _matchedCellsBuffer = new Vector2Int[16];

        // Buffer for new wall cells.
        private readonly CellColor?[] _wallSpawnBuffer;

        // Buffer array for finding colorislands
        // (used for counting cells in color region for making new cell color decision)
        bool[,] _islandsBuffer;

        // Array for tempopary storing neighbour cells during calculations.
        private readonly Cell[] _neighbourCellsArray = new Cell[Direction.FOUR_DIRECTIONS.Length];

        // Not to record game over before event that bring us here waiting for one frame
        private bool _handleGameOverNextFrame;

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
                MapWidth = mapWidth;
                MapHeight = mapHeight;
                ScrollTimeStep = scrollTimeStep;
                PlayerStartPosition = playerStartPosition;
                PlayerColor = playerColor;
                FrozenProjectileColor = frozenProjectileColor;
                ProjectileSpeed = projectileSpeed;
                LateralBricksStopProjectiles = lateralBricksStopProjectiles;
                ProjectilesCollideMapBounds = projectilesCollideMapBounds;
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
            this._gameSettings = gameSettings;
            this._cellPatterns = tetraminoPatterns;
            this._cellGenerator = cellGenerator;
            _wallSpawnBuffer = new CellColor?[gameSettings.MapHeight];

            _frameUpdateStream = _timeEventsBus.FrameUpdatePublisher;

            IObservable<int> scrollStepStream = _frameUpdateStream
                .Where(_ => _gameState.GamePhase is not GamePhase.Paused and not GamePhase.NotStarted)
                .Scan((acc, delta) => acc + delta)
                .Select(time => (int)(time / gameSettings.ScrollTimeStep))
                .DistinctUntilChanged()
                .Scan((0, 0), (oldStepsAndDelta, newSteps)
                    =>
                { return (newSteps, newSteps - oldStepsAndDelta.Item1); })
                .SelectMany(stepsWithDelta => Enumerable.Range(stepsWithDelta.Item1 - stepsWithDelta.Item2, stepsWithDelta.Item2));
            // Making TableScrollStream connectable Observable not to repeat side effects for each subscriber
            // (as new column generation).
            _tableScrollStream = scrollStepStream
                .Do(_ => this._cellGenerator.GenerateCells(_wallSpawnBuffer))
                .Select(steps => _wallSpawnBuffer).Share();

            inputEventProvider.GetInputStream().Subscribe(e => e.Apply(_timeEventsBus, _playerInputBus));

            _frameUpdateStream.Where(_ => _gameState.GamePhase is not GamePhase.Paused or GamePhase.NotStarted)
                .Subscribe(deltaTime =>
                {
                    handleProjectiles(deltaTime);
                });
            _frameUpdateStream.Subscribe(_ =>
            {
                if (_handleGameOverNextFrame)
                {
                    _handleGameOverNextFrame = false;
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
            _wallEliminationStream.SelectMany(
                wData => _frameUpdateStream.First().Select(_ => wData))
                .Subscribe(tpl => eliminateWall(tpl.Item1, tpl.Item2));

            _playerInputBus.PlayerMovementStream.Subscribe(movePlayer);
            _playerInputBus.PlayerRotateStream.Subscribe(handleRotation);
            _playerInputBus.PlayerShotStream.Subscribe(_ => handleShot());

            _frozenProjectilesInnerStream.Subscribe(handleNewCell);
            _playerInputBus.GameStartStream
            .Subscribe(tableSize => SetPhase(GamePhase.Started));
            _playerInputBus.GamePauseResumeStream.Subscribe(
                paused => SetPhase(paused ? GamePhase.Paused : GamePhase.Started)
            );


            resetGameState();
        }

        /// <summary> Sets new game phase. </summary>
        /// <returns>old phase</returns>
        GamePhase SetPhase(GamePhase gamePhase)
        {
            var oldPhase = _gameState.GamePhase;
            if (gamePhase == GamePhase.Started)
            {
                if (_gameState.GamePhase is GamePhase.NotStarted or GamePhase.GameOver)
                {
                    StartNewGame();
                }
                else if (_gameState.GamePhase is GamePhase.Paused)
                {
                    // unpause
                }
            }
            _gameState.SetGamePhase(gamePhase);
            _gamePhaseStream.OnNext(gamePhase);
            //Debug.Log($"Switching {oldPhase} to {gamePhase}");
            return oldPhase;
        }

        void StartNewGame()
        {
            resetGameState();
            _islandsBuffer = new bool[_gameSettings.MapWidth, _gameSettings.MapHeight];
            _gameStartedStream.OnNext(new Vector2Int(_gameSettings.MapWidth, _gameSettings.MapHeight));
            _playerTetrominoStream.OnNext(_gameState.PlayerTetromino);
        }

        void scrollBricks(IEnumerable<CellColor?> newWall)
        {
            if (_gameState.GamePhase is not GamePhase.GameOver)
            {
                if (!checkPlayerTetrominoCollisions(_gameState.PlayerTetromino, Vector2Int.right)
                    || isNewColumnCollidingPlayer(newWall))
                {
                    handleGameOver();
                }
            }
            _gameState.GameTable.ScrollLeft(newWall);

            bool isNewColumnCollidingPlayer(IEnumerable<CellColor?> newWall)
            {
                int y = 0;
                foreach (CellColor? cellColor in newWall)
                {
                    if (cellColor.HasValue
                        && _gameState.PlayerTetromino.Contains(v2i(_gameState.GameTable.Size.x - 1, y))
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
            _gameState = new GameState(
                new PlayerTetromino(_gameSettings.PlayerStartPosition, _gameSettings.PlayerColor),
                // Doubled quantity of table cells should be enough for keeping all particles on the screen
                new Projectile[_gameSettings.MapWidth * _gameSettings.MapHeight * 2],
                GamePhase.NotStarted,
                new ColorTable(v2i(_gameSettings.MapWidth, _gameSettings.MapHeight))
            );
        }

        void movePlayer(MovementInput input)
        {
            if (_gameState.GamePhase is not GamePhase.Started)
            { return; }

            Vector2Int dir = getMoveDirection(input);
            if (!checkPlayerTetrominoCollisions(_gameState.PlayerTetromino, dir))
            {
                _handleGameOverNextFrame = true;
                return;
            }
            _gameState.SetPlayerTetromino(_gameState.PlayerTetromino.WithPosition(calculateNewPlayerPosition(dir)));
            _playerTetrominoStream.OnNext(_gameState.PlayerTetromino);
            checkWallsAroundPlayerTetramino();
        }

        private void handleRotation(bool clockwise)
        {
            if (_gameState.GamePhase is not GamePhase.Started)
            { return; }

            PlayerTetromino rotatedTetramino = _gameState.PlayerTetromino.Rotate(clockwise);
            rotatedTetramino = fixPositionAfterRotation(rotatedTetramino);
            if (!checkPlayerTetrominoCollisions(rotatedTetramino))
            {
                _handleGameOverNextFrame = true;
                return;
            }
            _gameState.SetPlayerTetromino(rotatedTetramino);
            _playerTetrominoStream.OnNext(_gameState.PlayerTetromino);
            _rotationStream.OnNext(clockwise);

            // Arranges tetramino position if it exeeded map bounds after rotation
            PlayerTetromino fixPositionAfterRotation(PlayerTetromino tetramino)
            {
                var size = tetramino.Size;
                var position = tetramino.Position;
                Vector2Int fixedPosition = new Vector2Int(
                    Mathf.Clamp(position.x, 0, _gameState.GameTable.Size.x - size.x),
                    Mathf.Clamp(position.y, 0, _gameState.GameTable.Size.y - size.y)
                );
                return tetramino.WithPosition(fixedPosition);
            }
        }

        private void handleShot()
        {
            if (_gameState.GamePhase is not GamePhase.Started)
            { return; }

            _shotStream.OnNext(_gameState.PlayerTetromino.Direction);

            Vector2Int projectileStartPosition = _gameState.PlayerTetromino.Position
                + _gameState.PlayerTetromino.Muzzle;

            if (
                !isOutOfMapBounds(projectileStartPosition)
                && !_gameState.GameTable[projectileStartPosition].HasValue
            )
            {
                _gameState.IncNextProjectileInd();
                _gameState.Projectiles[_gameState.NextProjectileInd] =
                    new Projectile(projectileStartPosition, _gameState.PlayerTetromino.Direction);
                if (_gameState.NextProjectileInd >= _gameState.Projectiles.Length)
                {
                    _gameState.ResetNextProjectileInd();
                }
            }
        }

        void handleProjectiles(float deltaTime)
        {
            if (_gameState.GamePhase is GamePhase.Paused)
            { return; }

            for (int i = 0; i < _gameState.Projectiles.Length; i++)
            {
                Projectile projectile = _gameState.Projectiles[i];
                if (projectile.Active)
                {
                    bool flewAway = _gameSettings.ProjectilesCollideMapBounds
                        ? isOutOfHorizontalMapBounds(projectile.Position.x)
                        : isOutOfMapBounds(projectile.Position);
                    if (!flewAway)
                    {
                        if (shouldBeFrozen(projectile, deltaTime, out Vector2Int landPosition))
                        {
                            _frozenProjectilesInnerStream.OnNext(landPosition);
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
                            + projectile.Direction.toVector2() * _gameSettings.ProjectileSpeed * deltaTime;
                        _projectileCoordinatesStream.OnNext(projectile.Position);
                    }
                    // Writing updated projectile back to the array
                    _gameState.Projectiles[i] = projectile;
                }
            }
        }

        private bool shouldBeFrozen(Projectile projectile, float deltaTime, out Vector2Int landPosition)
        {
            landPosition = default;
            // checking integer position
            Vector2Int intPos = projectile.Position.toVector2Int();
            if ((_gameSettings.ProjectilesCollideMapBounds && isOutOfHorizontalMapBounds(intPos.x))
                || isOutOfMapBounds(intPos)
            )
            {
                return false;
            }

            // Checking each cell passed by projectile to decide does it collide with anything
            // If The prijectile is already above occupied cell then backpress the projectile back to freeze it in 
            // free space. During backpressing we should check does the projectile collides with the player tetromino
            // itself. In this case player tetromino goes down (game over)
            if (_gameState.GameTable[projectile.Position.toVector2Int()].HasValue)
            {
                // projectile cell is occupied already. Projectile should be backpressured
                return backPressureProjectile(projectile, out landPosition);
            }
            var farthestPosition = projectile.Position + projectile.Direction.toVector2() * _gameSettings.ProjectileSpeed * deltaTime;
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
                if (_gameSettings.LateralBricksStopProjectiles &&
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
                        && _gameState.GameTable[coordinates.x, coordinates.y].HasValue;
                }

                bool stoppedByMapBounds(Vector2Int destinationCoordinates)
                    => _gameSettings.ProjectilesCollideMapBounds
                    && isOutOfVerticalMapBounds(destinationCoordinates.y);
            }

            bool backPressureProjectile(Projectile projectile, out Vector2Int landPosition)
            {
                var initProjPos = projectile.Position.toVector2Int();
                landPosition = initProjPos;
                Vector2Int newPos = initProjPos - projectile.Direction;
                while (_gameState.GameTable[newPos].HasValue)
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
            if (_gameState.GamePhase is not GamePhase.Started)
            { return; }
            var playerPos = _gameState.PlayerTetromino.Position.x;
            for (int locX = 0; locX < _gameState.PlayerTetromino.Size.x; locX++)
            {
                sendWallToDestructionIfItsFilled(playerPos + locX, -Vector2Int.one);
            }
        }

        void handleGameOver()
        {
            if (_gameState.GamePhase is not GamePhase.GameOver)
            {
                // Transforming player tetromino to game table cells
                foreach (var plCell in _gameState.PlayerTetromino)
                {
                    if (!_gameState.GameTable[plCell].HasValue)
                    {
                        _gameState.GameTable[plCell] = _gameState.PlayerTetromino.Color;
                        _newCellStream.OnNext(new Cell(plCell, _gameState.PlayerTetromino.Color));
                    }
                }

                SetPhase(GamePhase.GameOver);
            }
        }

        private void handleNewCell(Vector2Int newCellCoordinates)
        {
            // check that new cell does not collide with player's tetromino
            if (_gameState.GamePhase is not GamePhase.GameOver
                && _gameState.PlayerTetromino.Contains(newCellCoordinates)
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
            if (_gameState.GamePhase is GamePhase.GameOver)
            {
                // Freezing projectiles on game over. no matter what color it should be
                _newCellStream.OnNext(new Cell(newCellCoordinates, _gameSettings.FrozenProjectileColor));
                _frozenProjectilesStream.OnNext(newCellCoordinates);
                return;
            }
            uint matchedCellsCount = addCellOfProperColorIfNoMatch(
                newCellCoordinates,
                _cellPatterns,
                (ColorTable table, CellColor color, Vector2Int neighbourCell)
                    => calculateColorScore(table, color, neighbourCell),
                _gameSettings.FrozenProjectileColor,
                _matchedCellsBuffer,
                out CellColor? patternColor
            );
            if (matchedCellsCount > 0)
            {
                for (int i = 0; i < matchedCellsCount; i++)
                {
                    eliminateCell(new Cell(_matchedCellsBuffer[i], patternColor.Value));
                }
            }
            else
            {
                // We added the cell into the table above, so it must have some color
                var newCellColor = _gameState.GameTable[newCellCoordinates].Value;
                _newCellStream.OnNext(new Cell(newCellCoordinates, newCellColor));
                _frozenProjectilesStream.OnNext(newCellCoordinates);
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
                    CellColor? col = _gameState.GameTable[cellPos + dir];
                    if (col.HasValue)
                    {
                        _neighbourCellsArray[neighbourCellsCount++] = new Cell(cellPos + dir, col.Value);
                    }
                }
            }

            uint matched = _gameState.GameTable.FindPattern(
                cellPatterns, cellPos, matchedCellsBuffer, _neighbourCellsArray, neighbourCellsCount, out patternColor
            );
            if (matched > 0)
            {
                // if there was a pattern match - going out to explode cells
                return matched;
            }

            // No pattern match, making decision what color should new cell have
            if (_neighbourCellsArray.Length == 0)
            {
                _gameState.GameTable[cellPos] = defaultColor;
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
                    var neighbour = _neighbourCellsArray[i];
                    if (color == neighbour.Color)
                    {
                        sameColorCounter++;
                        singleNeighbour = neighbour;
                    }
                }

                if (sameColorCounter > 1)
                {
                    // connect two samecoloured neighbour cells with the thitd andgo back
                    _gameState.GameTable[cellPos] = color;
                    return 0;
                }
                else if (sameColorCounter == 1)
                {
                    // more than one cell


                    uint score = calculateColorScore(_gameState.GameTable, color, singleNeighbour.Position);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        colorWinner = color;
                    }
                }
            }

            _gameState.GameTable[cellPos] = colorWinner;
            return 0;
        }

        private uint calculateColorScore(ColorTable table, CellColor color, Vector2Int neighbourCell)
        {
            // let's paint thenew cell with colour of the most bigger neighbour region.
            for (int x = 0; x < table.Size.x; x++)
            {
                for (int y = 0; y < table.Size.y; y++)
                {
                    _islandsBuffer[x, y] = false;
                }
            }

            return CountConnectedCells(table, color, neighbourCell, _islandsBuffer);
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
            var plCells = _gameState.PlayerTetromino.GetVerticalCells(wallX);
            // Compare buffered wall and new wall
            bool shouldBeEliminated = true;
            for (int y = 0; y < _gameState.GameTable.Size.y; ++y)
            {
                if (
                    v2i(wallX, y) != withProjectile
                    && !_gameState.GameTable[wallX, y].HasValue
                    && !plCells.Contains(v2i(wallX, y))
                )
                {
                    shouldBeEliminated = false;
                    break;
                }
            }
            if (shouldBeEliminated)
            {
                _wallEliminationStream.OnNext((wallX, withProjectile));
            }
            return shouldBeEliminated;
        }

        private void eliminateWall(int wallX, Vector2Int withProjectile)
        {
            var plCells = _gameState.PlayerTetromino.GetVerticalCells(wallX);
            // blocks destruction
            for (int y = 0; y < _gameState.GameTable.Size.y; y++)
            {
                var pos = v2i(wallX, y);
                if (!plCells.Contains(new Vector2Int(wallX, y)) || pos == withProjectile)
                {
                    CellColor? cellColor = pos != withProjectile
                        ? _gameState.GameTable[pos]
                        : _gameSettings.FrozenProjectileColor;
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
            _gameState.GameTable.RemoveCell(cell.Position);
            _eliminatedBricksStream.OnNext(cell);
            _gameState.IncScore();
            _scoreStream.OnNext(_gameState.Scores);
        }

        private Vector2Int calculateNewPlayerPosition(Vector2Int moveDir)
        {
            var newPlayerPos = _gameState.PlayerTetromino.Position + moveDir;
            newPlayerPos = new Vector2Int(
                Mathf.Clamp(newPlayerPos.x, 0, _gameState.GameTable.Size.x - _gameState.PlayerTetromino.Size.x),
                Mathf.Clamp(newPlayerPos.y, 0, _gameState.GameTable.Size.y - _gameState.PlayerTetromino.Size.y));
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
                if (!isOutOfMapBounds(cell + shift) && _gameState.GameTable[cell + shift].HasValue)
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfHorizontalMapBounds(float x) => _gameState.GameTable.IsOutOfHorizontalMapBounds(x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfVerticalMapBounds(float y) => _gameState.GameTable.IsOutOfVerticalMapBounds(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isOutOfMapBounds(Vector2 position)
            => _gameState.GameTable.IsOutOfMapBounds(position);

        class GameInputStreams : IGameInputBus
        {
            private readonly ISubject<MovementInput> _playerMovementStream = new Subject<MovementInput>();
            ISubject<MovementInput> IGameInputBus.PlayerMovementStream => _playerMovementStream;

            private readonly ISubject<Unit> _playerShotStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.PlayerShotStream => _playerShotStream;

            private readonly ISubject<bool> _playerRotateStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.PlayerRotateStream => _playerRotateStream;

            private readonly ISubject<Unit> _gameStartStream = new Subject<Unit>();
            ISubject<Unit> IGameInputBus.GameStartStream => _gameStartStream;

            private readonly ISubject<bool> _gamePauseResumeStream = new Subject<bool>();
            ISubject<bool> IGameInputBus.GamePauseResumeStream => _gamePauseResumeStream;

            public GameInputStreams() { }
        }

        class TimeEventBus : IGameTimeBus
        {
            public ISubject<float> FrameUpdatePublisher => _frameUpdateStream;
            private readonly ISubject<float> _frameUpdateStream = new Subject<float>();
        }
    }

    internal struct WallSpawnEnumerable : IEnumerable<CellColor?[]>
    {
        private readonly ICellGenerator _cellGenerator;
        private readonly CellColor?[] _wallSpawnBuffer;
        private readonly int _wallsCount;

        public WallSpawnEnumerable(ICellGenerator cellGenerator, CellColor?[] wallSpawnBuffer, int wallsCount)
        {
            _cellGenerator = cellGenerator;
            _wallSpawnBuffer = wallSpawnBuffer;
            _wallsCount = wallsCount;
        }


        public WallSpawnEnumerator GetEnumerator()
        {
            return new WallSpawnEnumerator(_cellGenerator, _wallSpawnBuffer, _wallsCount);
        }

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        IEnumerator<CellColor?[]> IEnumerable<CellColor?[]>.GetEnumerator() => this.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
    }

    internal struct WallSpawnEnumerator : IEnumerator<CellColor?[]>
    {

        private readonly ICellGenerator _cellGenerator;
        private readonly CellColor?[] _wallSpawnBuffer;
        private readonly int _wallsCount;

        bool _movedNext;
        bool _hasNext;
        int _currentWallInd;

        public WallSpawnEnumerator(ICellGenerator cellGenerator, CellColor?[] wallSpawnBuffer, int wallsCount)
        {
            _cellGenerator = cellGenerator;
            _wallSpawnBuffer = wallSpawnBuffer;
            _wallsCount = wallsCount;
            _movedNext = false;
            _hasNext = false;
            _currentWallInd = 0;
        }

        public CellColor?[] Current =>
            !_movedNext || !_hasNext
            ? throw new InvalidOperationException("No more elements to iterate!")
            : _wallSpawnBuffer;

        object IEnumerator.Current => Current;


        public bool MoveNext()
        {
            _movedNext = true;
            if (_wallsCount != 0 && _currentWallInd < _wallsCount)
            {
                _cellGenerator.GenerateCells(_wallSpawnBuffer);
                _currentWallInd++;
                _hasNext = true;
            }
            else
            {
                _hasNext = false;
            }
            return _hasNext;
        }

        public void Reset()
        {
            _movedNext = false;
            _hasNext = false;
            _currentWallInd = 0;
        }
        public void Dispose() { }
    }
}
