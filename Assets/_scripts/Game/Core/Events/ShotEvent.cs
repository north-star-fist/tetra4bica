using System;
using UniRx;

[Serializable]
public class ShotEvent : IGameInputEvent {
    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
        => inputBus.PlayerShotStream.OnNext(Unit.Default);
}
