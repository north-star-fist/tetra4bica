using Tetra4bica.Core;
using UnityEngine;
using VContainer;

namespace Tetra4bica.Init
{

    [CreateAssetMenu(
        fileName = "Cell Generator Installer",
        menuName = "Tetra4bica/DI Installers/Default Cell Generator"
    )]
    public class DefaultCellGeneratorInstaller : AScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<CellGenerator>(Lifetime.Scoped).As<ICellGenerator>();
        }

        class CellGenerator : ICellGenerator
        {
            // The class uses default interface cell generation
        }
    }
}
