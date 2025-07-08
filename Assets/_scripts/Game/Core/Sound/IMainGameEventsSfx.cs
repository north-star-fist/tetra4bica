
using System;
using Tetra4bica.Init;
using UniRx;

namespace Tetra4bica.Core {

    public interface IMainGameEventsSfx
    {
        void Setup(
            IObservable<Unit> gameStartedStream,
            IObservable<Unit> gameOverStream,
            IAudioSourceManager audioManager
        );
    }
}
