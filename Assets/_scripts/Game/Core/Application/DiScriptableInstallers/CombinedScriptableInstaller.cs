using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Tetra4bica.Init
{

    [CreateAssetMenu(
        menuName = "Tetra4bica/DI Installers/Combined Scriptable Installer",
        fileName = "Combined Scriptable Installer"
    )]
    public class CombinedScriptableInstaller : AScriptableInstaller
    {
        [SerializeField]
        private List<AScriptableInstaller> _installers;

        public override void Install(IContainerBuilder builder)
        {
            foreach (var installer in _installers)
            {
                installer.Install(builder);
            }
        }
    }
}
