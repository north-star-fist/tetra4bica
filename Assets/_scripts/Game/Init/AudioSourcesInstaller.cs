using System;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Init
{

    public class AudioSourcesInstaller : MonoInstaller
    {
        [SerializeField]
        private GroupPrefab[] audioSourcePrefabs;

        [Serializable]
        public class GroupPrefab
        {
            public AudioSourceId AudioGroup => audioGroup;
            public GameObject AudioSourcePrefab => audioSourcePrefab;

            [SerializeField]
            private AudioSourceId audioGroup;
            [SerializeField]
            private GameObject audioSourcePrefab;
        }

        public override void InstallBindings()
        {

            foreach (var groupPrefab in audioSourcePrefabs)
            {
                Container.Bind<AudioSource>().WithId(groupPrefab.AudioGroup)
                    .FromComponentInNewPrefab(groupPrefab.AudioSourcePrefab).AsTransient().NonLazy();
            }

        }
    }
}
