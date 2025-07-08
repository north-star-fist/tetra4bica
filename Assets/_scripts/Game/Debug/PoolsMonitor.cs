using Tetra4bica.Init;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace Tetra4bica.Debugging
{

    public class PoolsMonitor : MonoBehaviour
    {

        IObjectPool<GameObject> _bricksPool;
        IObjectPool<GameObject> _playerPool;
        IObjectPool<GameObject> _scorePool;
        IObjectPool<GameObject> _playerExplosionPool;
        IObjectPool<GameObject> _cellExplosionPool;

        [Inject]
        private IGameObjectPoolManager _poolManager;

        private void Start()
        {
            _bricksPool = _poolManager.GetPool(PoolId.GAME_CELLS);
            _playerPool = _poolManager.GetPool(PoolId.PLAYER_CELLS);
            _scorePool = _poolManager.GetPool(PoolId.SCORE_CELLS);
            _playerExplosionPool = _poolManager.GetPool(PoolId.PLAYER_EXPLOSION);
            _cellExplosionPool = _poolManager.GetPool(PoolId.WALL_CELL_EXPLOSION);
        }

        private void OnGUI()
        {
            printPoolState(10, 10, nameof(PoolId.GAME_CELLS), _bricksPool);
            printPoolState(10, 60, nameof(PoolId.PLAYER_CELLS), _playerPool);
            printPoolState(10, 110, nameof(PoolId.SCORE_CELLS), _scorePool);
            printPoolState(10, 160, nameof(PoolId.PLAYER_EXPLOSION), _playerExplosionPool);
            printPoolState(10, 210, nameof(PoolId.WALL_CELL_EXPLOSION), _cellExplosionPool);
        }

        private static void printPoolState(int screenX, int screenY, string poolId, IObjectPool<GameObject> pool)
        {
            ObjectPool<GameObject> objPool = ((ObjectPool<GameObject>)pool);
            GUI.Label(
                new Rect(screenX, screenY, 200, 50),

                $"Pool {poolId}: {objPool.CountAll.ToString()} " +
                $"({objPool.CountActive.ToString()}/{pool.CountInactive.ToString()})"
            );
        }
    }
}
