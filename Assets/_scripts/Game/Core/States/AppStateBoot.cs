using System;
using System.Threading;
using Sergei.Safonov.Unity;
using Sergei.Safonov.StateMachinery;
using Tetra4bica.Init;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Tetra4bica.Flow
{

    public class AppStateBoot : IState
    {
        private ISettingsManager _settingsManager;

        public string StateId => nameof(AppStateBoot);


        public AppStateBoot(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }


        public async Awaitable OnStateEnterAsync() {
        }

        public async Awaitable OnStateExitAsync() { }


        public async Awaitable<Type> StartAsync(CancellationToken cancelToken)
        {
            _settingsManager.ActivateSettings(_settingsManager.GetCurrentGameSettings());
            return typeof(AppStateGame);
        }
    }
}
