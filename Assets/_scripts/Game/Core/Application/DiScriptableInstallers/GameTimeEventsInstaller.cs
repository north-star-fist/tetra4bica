using System;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace Tetra4bica.Init
{

    [CreateAssetMenu(
        fileName = "Game Time Events Installer",
        menuName = "Tetra4bica/DI Installers/Time Events Installer"
    )]
    public class GameTimeEventsInstaller : AScriptableInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<GameTime>(Lifetime.Scoped).AsImplementedInterfaces();
        }

        private class GameTime : IGameTimeEvents, IInitializable, IDisposable
        {
            public IObservable<float> FrameUpdateStream => _frames;
            private readonly Subject<float> _frames = new Subject<float>();

            private bool _started;

            private readonly CompositeDisposable _disposables = new CompositeDisposable();
            private bool _disposed;

            public void Initialize()
            {
                Observable.EveryUpdate().Subscribe(_ =>
                {
                    if (_started)
                    {
                        _frames.OnNext(Time.deltaTime);
                    }
                }).AddTo(_disposables);
            }

            public void StartFrames()
            {
                _started = true;
            }

            public void StopFrames()
            {
                _started = false;
            }

            #region Disposable
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _disposables.Dispose();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    _disposed = true;
                }
            }
            #endregion
        }
    }
}
