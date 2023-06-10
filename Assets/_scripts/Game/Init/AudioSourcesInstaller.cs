using System;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Init {

    public class AudioSourcesInstaller : MonoInstaller {

        public GroupPrefab[] audioSourcePrefabs;

        [Serializable]
        public class GroupPrefab {
            public AudioSourceId audioGroup;
            public GameObject audioSourcePrefab;
        }

        public override void InstallBindings() {

            foreach (var groupPrefab in audioSourcePrefabs) {
                Container.Bind<AudioSource>().WithId(groupPrefab.audioGroup)
                    .FromComponentInNewPrefab(groupPrefab.audioSourcePrefab).AsTransient().NonLazy();
            }

        }
    }
}