using System.Collections.Generic;
using System;
using Tetra4bica.Init;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;
using Sergei.Safonov.Utility.Pool;
using UnityEngine.Pool;

namespace Tetra4bica.Flow {

    /// <summary>
    /// Starts the application registering <see cref="AppEntryPoint"/> through
    /// <see cref="VContainer.IContainerBuilder"/>.
    /// By the way in goes through provided
    /// <see cref="AScriptableInstaller"/>s and launches them.
    /// </summary>
    public class AppStarter : LifetimeScope {


        [SerializeField]
        private AScriptableInstaller[] _diInstallers;

        [SerializeField, FormerlySerializedAs("audioSourcePrefabs")]
        private GroupPrefab[] _audioSourcePrefabs;

        [SerializeField, FormerlySerializedAs("pools")]
        private PoolIdDescription[] _pools;


        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            if (_diInstallers != null)
            {
                foreach (var installer in _diInstallers)
                {
                    installer.Install(builder);
                }
            }

            builder.RegisterInstance(InitAudioSources()).As<IAudioSourceManager>();

            var poolManager = InitPoolManager();
            builder.RegisterInstance(poolManager).AsImplementedInterfaces();


            builder.RegisterEntryPoint<AppEntryPoint>();
        }

        private GameObjectPoolManager InitPoolManager()
        {
            Dictionary<PoolId, IObjectPool<GameObject>> pools = new();
            foreach (var pool in _pools)
            {
                /*
                Container.Bind<IObjectPool<GameObject>>().WithId(pool.PoolId)
                    .FromInstance(PoolManager.GetPoolSingleton(pool.PoolDescription)).AsCached().NonLazy();
                */
                pools.Add(pool.PoolId, PoolManager.GetPoolSingleton(pool.PoolDescription));
            }
            GameObjectPoolManager poolManager = new GameObjectPoolManager(pools);
            return poolManager;
        }

        private IAudioSourceManager InitAudioSources()
        {
            Dictionary<AudioSourceId, AudioSource> map = new();
            foreach (var groupPrefab in _audioSourcePrefabs)
            {
                /*
                Container.Bind<AudioSource>().WithId(groupPrefab.AudioGroup)
                    .FromComponentInNewPrefab(groupPrefab.AudioSourcePrefab).AsTransient().NonLazy();
                */

                var aSourceObj = Instantiate(groupPrefab.AudioSourcePrefab);
                GameObject.DontDestroyOnLoad(aSourceObj);
                map.Add(groupPrefab.AudioGroup, aSourceObj.GetComponent<AudioSource>());
            }

            return new AudioSourceManager(map);
        }


        [Serializable]
        public class PoolIdDescription
        {
            public PoolId PoolId => _poolId;
            public PoolDescriptionAsset PoolDescription => _poolDescription;

            [SerializeField, FormerlySerializedAs("poolId")]
            private PoolId _poolId;
            [SerializeField, FormerlySerializedAs("poolDescription")]
            private PoolDescriptionAsset _poolDescription;
        }

        [Serializable]
        public class GroupPrefab
        {
            [SerializeField, FormerlySerializedAs("audioGroup")]
            private AudioSourceId _audioGroup;
            [SerializeField, FormerlySerializedAs("audioSourcePrefab")]
            private GameObject _audioSourcePrefab;

            public AudioSourceId AudioGroup => _audioGroup;
            public GameObject AudioSourcePrefab => _audioSourcePrefab;
        }
    }
}
