using UnityEngine;
using VContainer;

namespace Tetra4bica.Init {
    public abstract class AScriptableInstaller : ScriptableObject {
        public abstract void Install(IContainerBuilder builder);
    }
}
