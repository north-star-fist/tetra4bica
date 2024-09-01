using System;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Init
{

    public class VisualSettingsInstaller : MonoInstaller
    {

        [SerializeField]
        private VisualSettings visualSettings;

        public override void InstallBindings()
        {
            if (visualSettings.CellSize <= 0)
            {
                throw new ArgumentException($"{nameof(visualSettings.CellSize)} is not positive");
            }
            Container.BindInstance(visualSettings).AsSingle();
        }
    }
}
