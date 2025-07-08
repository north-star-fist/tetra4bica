using Tetra4bica.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tetra4bica.Init
{

    public class VisualSettingsInstaller : MonoBehaviour, IVisualSettingsInstaller
    {

        [SerializeField, FormerlySerializedAs("visualSettings")]
        private VisualSettings _visualSettings;

        public IVisualSettings GetVisualSettings() {
            return _visualSettings;
        }
    }
}
