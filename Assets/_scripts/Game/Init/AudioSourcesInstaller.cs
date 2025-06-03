using System;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Init
{

    public class AudioSourcesInstaller : MonoInstaller
    {
        [SerializeField]
        private GroupPrefab[] _audioSourcePrefabs;

        [Serializable]
        public class GroupPrefab
        {
            [SerializeField]
            private AudioSourceId _audioGroup;
            [SerializeField]
            private GameObject _audioSourcePrefab;

            public AudioSourceId AudioGroup => _audioGroup;
            public GameObject AudioSourcePrefab => _audioSourcePrefab;
        }

        public override void InstallBindings()
        {

            foreach (var groupPrefab in _audioSourcePrefabs)
            {
                Container.Bind<AudioSource>().WithId(groupPrefab.AudioGroup)
                    .FromComponentInNewPrefab(groupPrefab.AudioSourcePrefab).AsTransient().NonLazy();
            }

        }
    }
}
