using System;
using Zenject;

namespace Tetra4bica.Init {

    public class VisualSettingsInstaller : MonoInstaller {

        public VisualSettings visualSettings;

        public override void InstallBindings() {
            if (visualSettings.cellSize <= 0) {
                throw new ArgumentException($"{nameof(visualSettings.cellSize)} is not positive");
            }
            Container.BindInstance(visualSettings).AsSingle();
        }
    }
}