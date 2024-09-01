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
        private PoolIdDescription[] pools;

        public override void InstallBindings()
        {
            foreach (var pool in pools)
            {
                Container.Bind<IObjectPool<GameObject>>().WithId(pool.PoolId)
                    .FromInstance(PoolManager.GetPoolSingleton(pool.PoolDescription)).AsCached().NonLazy();
            }
        }

        [Serializable]
        public class PoolIdDescription
        {
            public PoolId PoolId => poolId;
            public PoolDescriptionAsset PoolDescription => poolDescription;

            [SerializeField]
            private PoolId poolId;
            [SerializeField]
            private PoolDescriptionAsset poolDescription;
        }
    }
}
