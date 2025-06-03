using System;
using UniRx;

namespace Tetra4bica.Core
{
    [Serializable]
    public class ShotEvent : IGameInputEvent
    {
        public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
            => inputBus.PlayerShotStream.OnNext(Unit.Default);
    }
}
