using System;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.Init
{

    public class VisualSettingsInstaller : MonoInstaller
    {

        [SerializeField, FormerlySerializedAs("visualSettings")]
        private VisualSettings _visualSettings;

        public override void InstallBindings()
        {
            if (_visualSettings.CellSize <= 0)
            {
                throw new ArgumentException($"{nameof(_visualSettings.CellSize)} is not positive");
            }
            Container.BindInstance(_visualSettings).AsSingle();
        }
    }
}
