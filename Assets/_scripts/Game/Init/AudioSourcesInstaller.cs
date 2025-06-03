using System;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.Init
{

    public class AudioSourcesInstaller : MonoInstaller
    {
        [SerializeField, FormerlySerializedAs("audioSourcePrefabs")]
        private GroupPrefab[] _audioSourcePrefabs;

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
