using System;
using UniRx;

[Serializable]
public class StartNewGameEvent : IGameInputEvent {
    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
        => inputBus.GameStartStream.OnNext(Unit.Default);
}
