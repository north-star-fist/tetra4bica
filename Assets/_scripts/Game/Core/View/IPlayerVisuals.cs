using Tetra4bica.Init;

namespace Tetra4bica.Core
{
    public interface IPlayerVisuals
    {
        public void Setup(
            IGameEvents gameEvents,
            GameLogic.GameSettings gameSettings,
            IVisualSettings visualSettings,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager
        );
    }
}
