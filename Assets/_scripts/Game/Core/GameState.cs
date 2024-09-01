
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
        public PlayerTetromino PlayerTetromino => playerTetromino;
        /// <summary>
        /// Set of projectiles on the map.
        /// </summary>
        public Projectile[] Projectiles => projectiles;
        public uint NextProjectileInd => nextProjectileInd;
        public GamePhase GamePhase => gamePhase;
        public ColorTable GameTable => gameTable;
        public uint Scores => scores;

        private PlayerTetromino playerTetromino;
        private Projectile[] projectiles;
        private uint nextProjectileInd;
        private GamePhase gamePhase;
        private ColorTable gameTable;
        private uint scores;

        public GameState(
            PlayerTetromino playerTetromino,
            Projectile[] projectiles,
            GamePhase gamePhase,
            ColorTable gameTable
        )
        {
            this.playerTetromino = playerTetromino;
            this.projectiles = projectiles;
            this.gamePhase = gamePhase;
            this.gameTable = gameTable;
            nextProjectileInd = 0;
            scores = 0;
        }

        internal void IncScore() => scores++;
        internal void SetPlayerTetromino(PlayerTetromino newTetramino) => this.playerTetromino = newTetramino;

        internal void IncNextProjectileInd() => nextProjectileInd++;
        internal void ResetNextProjectileInd() => nextProjectileInd = 0;
        internal void SetGamePhase(GamePhase gamePhase) => this.gamePhase = gamePhase;
    }
}
