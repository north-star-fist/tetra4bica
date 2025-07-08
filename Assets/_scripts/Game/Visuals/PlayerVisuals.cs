using System;
using System.Threading.Tasks;
using Sergei.Safonov.Audio;
using Sergei.Safonov.Utility;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Tetra4bica.Graphics
{
    public class PlayerVisuals : MonoBehaviour, IPlayerVisuals
    {
        // It's tetromino - so it is 4.
        private const int PLAYER_TETRAMINO_CELL_COUNT = 4;

        [SerializeField, FormerlySerializedAs("playerDeathSfx")]
        private AudioResource _playerDeathSfx;
        [SerializeField, FormerlySerializedAs("playerShotSfx")]
        private AudioResource _playerShotSfx;
        [SerializeField, FormerlySerializedAs("playerRotateSfx")]
        private AudioResource _playerRotateSfx;


        private AudioSource _playerAudioSource;


        private IVisualSettings _visualSettings;


        private IObjectPool<GameObject> _playerCellPool;
        private IObjectPool<GameObject> _playerDeathParticlesPool;


        private readonly GameCell[] _playerCells = new GameCell[PLAYER_TETRAMINO_CELL_COUNT];


        public void Setup(
            IGameEvents gameEvents,
            GameLogic.GameSettings gameSettings,
            IVisualSettings visualSettings,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager
        )
        {
            releasePlayerCells();
            releaseDeathParticles();

            _playerAudioSource = audioManager.GetAudioSource(AudioSourceId.SoundEffects);
            _playerCellPool = poolManager.GetPool(PoolId.PLAYER_CELLS);
            _playerDeathParticlesPool = poolManager.GetPool(PoolId.PLAYER_EXPLOSION);
            _visualSettings = visualSettings;

            var gameOverStream = gameEvents.GamePhaseStream
                .Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default);

            gameEvents.PlayerTetrominoStream.Subscribe(RenderPlayer);
            gameOverStream.WithLatestFrom(gameEvents.PlayerTetrominoStream, (_, tetromino) => tetromino)
                .Subscribe(t => animatePlayerDeath(t));

            gameOverStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerDeathSfx)
            );
            gameEvents.ShotStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerShotSfx)
            );
            gameEvents.RotationStream.Subscribe(
                _ => SoundUtils.PlaySound(_playerAudioSource, _playerRotateSfx)
            );


            for (int i = 0; i < PLAYER_TETRAMINO_CELL_COUNT; i++)
            {
                _playerCells[i] = _playerCellPool.Get().GetComponent<GameCell>();
                if (_playerCells[i] == null)
                {
                    throw new MissingComponentException($"No {nameof(GameCell)} component found");
                }
                _playerCells[i].gameObject.SetActive(false);
                _playerCells[i].SetColor(gameSettings.PlayerColor);
            }

            void releasePlayerCells()
            {
                if (_playerDeathParticlesPool != null && _playerCells != null)
                {
                    for (int i = 0; i < _playerCells.Length; i++)
                    {
                        _playerCellPool.Release(_playerCells[i].gameObject);
                    }
                }
            }

            void releaseDeathParticles()
            {
                // these particles return to their pool theirself, so we can just leave
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
                var plCellComp = _playerCells[playerCellsCounter];
                Vector2 cellShift = new Vector2(
                    plCell.x * _visualSettings.CellSize,
                    plCell.y * _visualSettings.CellSize
                );
                plCellComp.transform.position =
                    _visualSettings.BottomLeftPoint + (Vector3)cellShift;
                plCellComp.gameObject.SetActive(true);
                playerCellsCounter++;
            }
        }

        private void animatePlayerDeath(PlayerTetromino tetromino)
        {
            if (_playerDeathParticlesPool != null)
            {
                explodePlayerCells(tetromino, _playerDeathParticlesPool);
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
                cell.gameObject.SetActive(false);
            }
        }
    }
}
