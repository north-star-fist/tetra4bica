using Sergei.Safonov.Utility;
using System;
using System.Threading.Tasks;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Graphics {
    public class PlayerVisuals : MonoBehaviour {

        [Inject]
        IGameEvents gameEvents;
        [Inject]
        GameLogic.GameSettings gameSettings;
        [Inject]
        VisualSettings visualSettings;

        [Inject(Id = PoolId.PLAYER_CELLS)]
        IObjectPool<GameObject> playerCellPool;
        [Inject(Id = PoolId.PLAYER_EXPLOSION)]
        IObjectPool<GameObject> playerDeathParticlesPool;


        // It's tetromino - so it is 4.
        private const int PLAYER_TETRAMINO_CELL_COUNT = 4;

        PlayerVisuals backComponent;

        private GameObject[] playerCells = new GameObject[PLAYER_TETRAMINO_CELL_COUNT];

        private void Awake() {
            Setup(
                this,
                gameEvents.PlayerTetrominoStream,
                gameEvents.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            );
        }

        void Setup(
            PlayerVisuals backComponent,
            IObservable<PlayerTetromino> playerTetrominoObservable,
            IObservable<Unit> gameOverObservable
        ) {
            this.backComponent = backComponent;
            playerTetrominoObservable.Subscribe(renderPlayer);
            gameOverObservable.WithLatestFrom(playerTetrominoObservable, (_, tetromino) => tetromino)
                .Subscribe(t => _ = animatePlayerDeath(t));

            playerCellPool = backComponent.playerCellPool;
            for (int i = 0; i < PLAYER_TETRAMINO_CELL_COUNT; i++) {
                playerCells[i] = playerCellPool.Get();
                playerCells[i].SetActive(false);
                SpriteRenderer renderer = playerCells[i].GetComponent<SpriteRenderer>();
                if (renderer != null) { renderer.color = Cells.ToUnityColor(backComponent.gameSettings.playerColor); }
            }
        }

        public void renderPlayer(PlayerTetromino tetramino) {
            Vector2Int playerBasePosition = tetramino.Position;
            // If player has more than 4 cells - we just ignore remaining cells and do not render them.
            int playerCellsCounter = 0;
            var allPlayerCells = tetramino;
            foreach (var plCell in allPlayerCells) {
                if (playerCellsCounter >= PLAYER_TETRAMINO_CELL_COUNT) {
                    break;
                }
                GameObject plCellObj = playerCells[playerCellsCounter];
                Vector2 cellShift = new Vector2(
                    plCell.x * backComponent.visualSettings.cellSize,
                    plCell.y * backComponent.visualSettings.cellSize
                );
                plCellObj.transform.position =
                    backComponent.visualSettings.BottomLeftPoint + (Vector3)cellShift;
                plCellObj.SetActive(true);
                playerCellsCounter++;
            }
        }

        private async Task animatePlayerDeath(PlayerTetromino tetromino) {
            if (backComponent.playerDeathParticlesPool != null) {
                IObjectPool<GameObject> particlesPool = backComponent.playerDeathParticlesPool;
                explodePlayerCells(tetromino, particlesPool);
            }
            disableVisuals();
        }

        private static void explodePlayerCells(PlayerTetromino tetromino, IObjectPool<GameObject> particlesPool) {
            foreach (var plCell in tetromino) {
                GameObject psObj = particlesPool.Get();
                ParticleSystem ps = psObj.GetComponent<ParticleSystem>();
                if (ps != null) {
                    // TODO scale
                    psObj.transform.position = plCell.toVector3();
                    psObj.SetActive(true);
                    ps.Play();
                } else {
                    Debug.LogError("Player explosion does not have ParticleSystem attached!");
                }
            }
        }

        private void disableVisuals() {
            foreach (var cell in playerCells) {
                cell.SetActive(false);
            }
        }
    }
}