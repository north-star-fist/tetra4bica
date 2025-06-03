using System;
using UniRx;

namespace Tetra4bica.Core
{
    [Serializable]
    public class StartNewGameEvent : IGameInputEvent
    {
        public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
            => inputBus.GameStartStream.OnNext(Unit.Default);
    }
}
