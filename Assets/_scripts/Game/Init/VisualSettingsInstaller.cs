using System;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Init
{

    public class VisualSettingsInstaller : MonoInstaller
    {

        [SerializeField]
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
