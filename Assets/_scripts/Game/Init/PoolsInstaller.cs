using System;
using Sergei.Safonov.Utility.Pool;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Init
{

    public class PoolsInstaller : MonoInstaller
    {
        [SerializeField]
        private PoolIdDescription[] _pools;

        public override void InstallBindings()
        {
            foreach (var pool in _pools)
            {
                Container.Bind<IObjectPool<GameObject>>().WithId(pool.PoolId)
                    .FromInstance(PoolManager.GetPoolSingleton(pool.PoolDescription)).AsCached().NonLazy();
            }
        }

        [Serializable]
        public class PoolIdDescription
        {
            public PoolId PoolId => _poolId;
            public PoolDescriptionAsset PoolDescription => _poolDescription;

            [SerializeField]
            private PoolId _poolId;
            [SerializeField]
            private PoolDescriptionAsset _poolDescription;
        }
    }
}
