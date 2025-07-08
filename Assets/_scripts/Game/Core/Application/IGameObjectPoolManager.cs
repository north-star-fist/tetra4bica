using UnityEngine;
using UnityEngine.Pool;

namespace Tetra4bica.Init
{
    public interface IGameObjectPoolManager
    {
        public IObjectPool<GameObject> GetPool(PoolId poolId);
    }
}
