
using Tetra4bica.Core;
using Tetra4bica.Init;

namespace Tetra4bica.Graphics
{
    public interface IColorTableView
    {
        public void Setup(
            IGameEvents gameEvents,
            IVisualSettings visualSettings,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager
        );
    }
}
