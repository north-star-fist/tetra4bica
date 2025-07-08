using Sergei.Safonov.Unity.SceneManagement;
using Tetra4bica.Core;
using Tetra4bica.Flow;
using UnityEngine;
using VContainer;
using static Tetra4bica.Core.GameLogic;

namespace Tetra4bica.Init
{

    /// <summary>
    /// Installer that configures <see cref="IAppFlow"/> and registers it in DI container.
    /// </summary>
    [CreateAssetMenu(menuName = "Tetra4bica/DI Installers/AppFlow", fileName = "AppFlow Installer")]
    public class AppFlowInstaller : AScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<AppFlow>(Lifetime.Scoped).As<IAppFlow>();

            builder.RegisterBuildCallback(container =>
            {
                // Creating App State Machine when DI Context is ready
                var appFlow = container.Resolve<IAppFlow>();

                var settingsManager = container.Resolve<ISettingsManager>();
                appFlow.RegisterState(new AppStateBoot(settingsManager));
                var sceneManager = container.Resolve<ISceneManager>();
                var gameSettnigs = container.Resolve<GameSettings>();
                ICellGenerator cellGenerator = container.Resolve<ICellGenerator>();
                ICellPatterns tetraminoPatterns = container.Resolve<ICellPatterns>();
                IGameTimeEvents timeEvents = container.Resolve<IGameTimeEvents>();
                
                IAudioSourceManager audioManager = container.Resolve<IAudioSourceManager>();
                IGameObjectPoolManager poolManager = container.Resolve<IGameObjectPoolManager>();
                AppStateGame gameState = new AppStateGame(
                    sceneManager,
                    settingsManager,
                    poolManager,
                    audioManager,

                    gameSettnigs,
                    tetraminoPatterns,
                    cellGenerator,
                    timeEvents
                );
                appFlow.RegisterState(gameState);
            });
        }
    }
}
