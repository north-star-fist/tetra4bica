using Tetra4bica.Init;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Debugging
{

    public class PoolsMonitor : MonoBehaviour
    {

        [Inject(Id = PoolId.GAME_CELLS)]
        IObjectPool<GameObject> bricksPool;

        [Inject(Id = PoolId.PLAYER_CELLS)]
        IObjectPool<GameObject> playerPool;

        [Inject(Id = PoolId.SCORE_CELLS)]
        IObjectPool<GameObject> scorePool;

        [Inject(Id = PoolId.PLAYER_EXPLOSION)]
        IObjectPool<GameObject> playerExplosionPool;

        [Inject(Id = PoolId.WALL_CELL_EXPLOSION)]
        IObjectPool<GameObject> cellExplosionPool;

        private void OnGUI()
        {
            printPoolState(10, 10, nameof(PoolId.GAME_CELLS), bricksPool);
            printPoolState(10, 60, nameof(PoolId.PLAYER_CELLS), playerPool);
            printPoolState(10, 110, nameof(PoolId.SCORE_CELLS), scorePool);
            printPoolState(10, 160, nameof(PoolId.PLAYER_EXPLOSION), playerExplosionPool);
            printPoolState(10, 210, nameof(PoolId.WALL_CELL_EXPLOSION), cellExplosionPool);
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
