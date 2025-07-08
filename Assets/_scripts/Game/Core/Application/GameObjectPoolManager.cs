using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Tetra4bica.Init
{
    public class GameObjectPoolManager : IGameObjectPoolManager
    {
        private readonly Dictionary<PoolId, IObjectPool<GameObject>> _pools;

        public GameObjectPoolManager(Dictionary<PoolId, IObjectPool<GameObject>> pools)
        {
            _pools = new Dictionary<PoolId, IObjectPool<GameObject>>(pools);
        }

        public IObjectPool<GameObject> GetPool(PoolId poolId) => _pools.TryGetValue(poolId, out var p) ? p : null;
    }
}
