using System;
using System.Collections.Generic;
using Sergei.Safonov.Audio;
using Sergei.Safonov.Utility;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Graphics
{

    public class ColorTableView : MonoBehaviour, IColorTableView
    {

        [SerializeField, FormerlySerializedAs("wallDestructionSfx")]
        private AudioResource _wallDestructionSfx;


        private IVisualSettings _visualSettings;
        private IGameObjectPoolManager _poolManager;


        // Brick instances on the map.
        private readonly Dictionary<Vector2Int, GameCell> _brickMap = new Dictionary<Vector2Int, GameCell>();

        private Vector2Int _mapSize;

        private bool _started;

        private IObjectPool<GameObject> _bricksPool;
        private IObjectPool<GameObject> _wallBrickExplosionParticlesPool;


        private AudioSource _audioSource;
        

        public void Setup(
            IGameEvents gameEvents,
            IVisualSettings visualSettings,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager
        )
        {
            _visualSettings = visualSettings;
            _poolManager = poolManager;
            gameEvents.TableScrollStream.Subscribe(scrollMap).AddTo(this);
            gameEvents.NewCellStream.Subscribe((c) => updateBrickVisuals(c)).AddTo(this);
            gameEvents.EliminatedBricksStream.Subscribe(
                (cell) => launchDestroyedBrickAnimation(cell.Position, cell.Color)
            ).AddTo(this);
            gameEvents.GameStartedStream.Subscribe(mapSize =>
            {
                if (this._mapSize == mapSize)
                {
                    eraseAll();
                    return;
                }
                this._mapSize = mapSize;
                instantiateBricks(
                    mapSize.x, mapSize.y,
                    _visualSettings.BottomLeftPoint
                );
                eraseAll();
                _started = true;
            }).AddTo(this);

            gameEvents.EliminatedBricksStream.Subscribe(
                _ => SoundUtils.PlaySound(_audioSource, _wallDestructionSfx)
            ).AddTo(this);
            _audioSource = audioManager.GetAudioSource(AudioSourceId.SoundEffects);

            _bricksPool = _poolManager.GetPool(PoolId.GAME_CELLS);
            _wallBrickExplosionParticlesPool = _poolManager.GetPool(PoolId.WALL_CELL_EXPLOSION);
        }

        private void eraseAll()
        {
            for (int x = 0; x < _mapSize.x; x++)
            {
                for (int y = 0; y < _mapSize.y; y++)
                {
                    eraseCell(v2i(x, y));
                }
            }
        }

        private void instantiateBricks(int width, int height, Vector2 bottomLeftTunnelPoint)
        {
            foreach (var brick in _brickMap.Values)
            {
                _bricksPool.Release(brick.gameObject);
            }
            _brickMap.Clear();
            for (int x = 0; x < width; x++)
            {
                Vector2 wallSpawnPoint = new Vector2(
                    bottomLeftTunnelPoint.x + x * _visualSettings.CellSize,
                    bottomLeftTunnelPoint.y
                );
                for (int y = 0; y < height; y++)
                {
                    GameObject brickInstance = instantiateBrick(wallSpawnPoint, y);
                    GameCell spriteRenderer = brickInstance.GetComponent<GameCell>()
                        ?? throw new Exception($"Cell prefab does not contain {typeof(GameCell)} component!");
                    _brickMap[v2i(x, y)] = spriteRenderer;
                }
            }

            GameObject instantiateBrick(Vector2 wallSpawnPoint, int y)
            {
                Vector3 brickPosition = new Vector2(
                    wallSpawnPoint.x,
                    wallSpawnPoint.y + _visualSettings.CellSize * y
                );
                var brickInstance = _bricksPool.Get();
                brickInstance.transform.position = brickPosition;
                brickInstance.transform.parent = _visualSettings.BricksParent;
                //brickInstance.SetActive(true);
                return brickInstance;
            }
        }

        private void scrollMap(IEnumerable<CellColor?> newWall)
        {
            if (!_started)
            {
                Debug.LogError("Trying to scroll the table before the game started!");
                return;
            }
            for (int x = 0; x < _mapSize.x - 1; x++)
            {
                for (int y = 0; y < _mapSize.y; y++)
                {
                    var cell = _brickMap[v2i(x, y)];
                    var cellToTheRight = _brickMap[v2i(x + 1, y)];
                    cell.SetColor(cellToTheRight.CellColor);
                }
            }
            {
                int y = 0;
                foreach (var cell in newWall)
                {
                    if (y >= _mapSize.y)
                    {
                        Debug.LogError("Received more cells in wall than the map height!");
                        break;
                    }
                    Vector2Int xy = v2i(_mapSize.x - 1, y);
                    if (cell.HasValue)
                    {
                        updateBrickVisuals(new Cell(xy, cell.Value));
                    }
                    else
                    {
                        eraseCell(xy);
                    }
                    y++;
                }
            }
        }

        private void updateBrickVisuals(Cell cell)
        {
            _brickMap[cell.Position].SetColor(cell.Color);
        }

        private void launchDestroyedBrickAnimation(Vector2Int xy, CellColor eliminatedCellColor)
        {

            eraseCell(xy);
            renderWallBrickExplosion(xy);

            void renderWallBrickExplosion(Vector2Int xy)
            {
                GameObject cellExplParticleSystemObj = _wallBrickExplosionParticlesPool.Get();
                // TODO scale
                cellExplParticleSystemObj.transform.position = xy.toVector3();
                cellExplParticleSystemObj.SetActive(true);
                ParticleSystem ps = cellExplParticleSystemObj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }

        private void eraseCell(Vector2Int xy)
        {
            var cell = _brickMap[xy];
            cell.SetColor(null);
            //spriteRenderer.gameObject.SetActive(false);
        }
    }
}
