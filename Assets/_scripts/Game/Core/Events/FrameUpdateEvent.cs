using System;

[Serializable]
public class FrameUpdateEvent : IGameInputEvent {

    readonly public float deltaTime;

    public FrameUpdateEvent(float deltaTime) {
        this.deltaTime = deltaTime;
    }

    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus _)
        => timeEventsBus.FrameUpdatePublisher.OnNext(deltaTime);
}
