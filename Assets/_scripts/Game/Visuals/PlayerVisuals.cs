using System;
using System.Threading.Tasks;
using Sergei.Safonov.Utility;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Graphics
{
    public class PlayerVisuals : MonoBehaviour
    {

        [Inject]
        private IGameEvents _gameEvents;
        [Inject]
        private GameLogic.GameSettings _gameSettings;
        [Inject]
        private VisualSettings _visualSettings;

        [Inject(Id = PoolId.PLAYER_CELLS)]
        private IObjectPool<GameObject> _playerCellPool;
        [Inject(Id = PoolId.PLAYER_EXPLOSION)]
        private IObjectPool<GameObject> _playerDeathParticlesPool;


        // It's tetromino - so it is 4.
        private const int PLAYER_TETRAMINO_CELL_COUNT = 4;

        private PlayerVisuals _backComponent;

        private readonly GameObject[] _playerCells = new GameObject[PLAYER_TETRAMINO_CELL_COUNT];

        private void Awake()
        {
            Setup(
                this,
                _gameEvents.PlayerTetrominoStream,
                _gameEvents.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            );
        }

        void Setup(
            PlayerVisuals backComponent,
            IObservable<PlayerTetromino> playerTetrominoObservable,
            IObservable<Unit> gameOverObservable
        )
        {
            this._backComponent = backComponent;
            playerTetrominoObservable.Subscribe(RenderPlayer);
            gameOverObservable.WithLatestFrom(playerTetrominoObservable, (_, tetromino) => tetromino)
                .Subscribe(t => _ = animatePlayerDeath(t));

            _playerCellPool = backComponent._playerCellPool;
            for (int i = 0; i < PLAYER_TETRAMINO_CELL_COUNT; i++)
            {
                _playerCells[i] = _playerCellPool.Get();
                _playerCells[i].SetActive(false);
                SpriteRenderer renderer = _playerCells[i].GetComponent<SpriteRenderer>();
                if (renderer != null)
                { renderer.color = Cells.ToUnityColor(backComponent._gameSettings.PlayerColor); }
            }
        }

        public void RenderPlayer(PlayerTetromino tetramino)
        {
            Vector2Int playerBasePosition = tetramino.Position;
            // If player has more than 4 cells - we just ignore remaining cells and do not render them.
            int playerCellsCounter = 0;
            var allPlayerCells = tetramino;
            foreach (var plCell in allPlayerCells)
            {
                if (playerCellsCounter >= PLAYER_TETRAMINO_CELL_COUNT)
                {
                    break;
                }
                GameObject plCellObj = _playerCells[playerCellsCounter];
                Vector2 cellShift = new Vector2(
                    plCell.x * _backComponent._visualSettings.CellSize,
                    plCell.y * _backComponent._visualSettings.CellSize
                );
                plCellObj.transform.position =
                    _backComponent._visualSettings.BottomLeftPoint + (Vector3)cellShift;
                plCellObj.SetActive(true);
                playerCellsCounter++;
            }
        }

        private async Task animatePlayerDeath(PlayerTetromino tetromino)
        {
            if (_backComponent._playerDeathParticlesPool != null)
            {
                IObjectPool<GameObject> particlesPool = _backComponent._playerDeathParticlesPool;
                explodePlayerCells(tetromino, particlesPool);
            }
            disableVisuals();
        }

        private static void explodePlayerCells(PlayerTetromino tetromino, IObjectPool<GameObject> particlesPool)
        {
            foreach (var plCell in tetromino)
            {
                GameObject psObj = particlesPool.Get();
                ParticleSystem ps = psObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    // TODO scale
                    psObj.transform.position = plCell.toVector3();
                    psObj.SetActive(true);
                    ps.Play();
                }
                else
                {
                    Debug.LogError("Player explosion does not have ParticleSystem attached!");
                }
            }
        }

        private void disableVisuals()
        {
            foreach (var cell in _playerCells)
            {
                cell.SetActive(false);
            }
        }
    }
}
