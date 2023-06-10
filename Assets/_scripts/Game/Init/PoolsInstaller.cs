using Sergei.Safonov.Utility.Pool;
using System;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Init {

    public class PoolsInstaller : MonoInstaller {

        public PoolIdDescription[] pools;

        public override void InstallBindings() {
            foreach (var pool in pools) {
                Container.Bind<IObjectPool<GameObject>>().WithId(pool.poolId)
                    .FromInstance(PoolManager.GetPoolSingleton(pool.poolDescription)).AsCached().NonLazy();
            }
        }

        [Serializable]
        public class PoolIdDescription {
            public PoolId poolId;
            public PoolDescriptionAsset poolDescription;
        }
    }
}