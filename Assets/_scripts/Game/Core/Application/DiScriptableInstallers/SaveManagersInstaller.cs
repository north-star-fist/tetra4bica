using Sergei.Safonov.Persistence;
using UnityEngine;
using VContainer;

namespace Tetra4bica.Init
{

    [CreateAssetMenu(fileName = "Save Managers Installer", menuName = "Tetra4bica/DI Installers/Save Managers")]
    public class SaveManagersInstaller : AScriptableInstaller
    {
        [SerializeField]
        private string _dataFolder = "game_data";

        public override void Install(IContainerBuilder builder)
        {
            // General save manager
            builder.Register<JsonFileSaveManager>(Lifetime.Singleton).WithParameter(_dataFolder).As<ISaveManager>();
        }

        private void OnValidate()
        {
            // TODO: verify the folder name
        }
    }
}
