
using System;

namespace Tetra4bica.Core
{

    public enum GamePhase
    {
        NotStarted,
        Started,
        GameOver,
        Paused,
    }

    public struct GameState
    {
        /// <summary>
        /// Current player tetramino
        /// </summary>
        public PlayerTetromino PlayerTetromino => _playerTetromino;
        /// <summary>
        /// Set of projectiles on the map.
        /// </summary>
        public Projectile[] Projectiles => _projectiles;
        public uint NextProjectileInd => _nextProjectileInd;
        public GamePhase GamePhase => _gamePhase;
        public ColorTable GameTable => _gameTable;
        public uint Scores => _scores;

        private PlayerTetromino _playerTetromino;
        private readonly Projectile[] _projectiles;
        private uint _nextProjectileInd;
        private GamePhase _gamePhase;
        private readonly ColorTable _gameTable;
        private uint _scores;

        public GameState(
            PlayerTetromino playerTetromino,
            Projectile[] projectiles,
            GamePhase gamePhase,
            ColorTable gameTable
        )
        {
            this._playerTetromino = playerTetromino;
            this._projectiles = projectiles;
            this._gamePhase = gamePhase;
            this._gameTable = gameTable;
            _nextProjectileInd = 0;
            _scores = 0;
        }

        internal void IncScore() => _scores++;
        internal void SetPlayerTetromino(PlayerTetromino newTetramino) => this._playerTetromino = newTetramino;

        internal void IncNextProjectileInd() => _nextProjectileInd++;
        internal void ResetNextProjectileInd() => _nextProjectileInd = 0;
        internal void SetGamePhase(GamePhase gamePhase) => this._gamePhase = gamePhase;
    }
}
