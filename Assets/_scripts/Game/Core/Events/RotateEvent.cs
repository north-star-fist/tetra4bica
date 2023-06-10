using System;

[Serializable]
public class RotateEvent : IGameInputEvent {

    private readonly bool clockwise;

    public RotateEvent(bool clockwise) {
        this.clockwise = clockwise;
    }

    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
        => inputBus.PlayerRotateStream.OnNext(clockwise);
}
