namespace Tetra4bica.Core {

    public enum GamePhase {
        NotStarted,
        Started,
        GameOver,
        Paused,
    }

    public struct GameState {
        // Current player tetramino
        public PlayerTetromino playerTetromino;
        // Set of projectiles on the map.
        public Projectile[] projectiles;
        public uint nextProjectileInd;
        public GamePhase gamePhase;
        public ColorTable gameTable;
        public uint scores;

        public GameState(
            PlayerTetromino playerTetromino,
            Projectile[] projectiles,
            GamePhase gamePhase,
            ColorTable gameTable
        ) {
            this.playerTetromino = playerTetromino;
            this.projectiles = projectiles;
            this.gamePhase = gamePhase;
            this.gameTable = gameTable;
            nextProjectileInd = 0;
            scores = 0;
        }
    }
}