using System;

[Serializable]
public class FrameUpdateEvent : IGameInputEvent
{

    readonly public float DeltaTime;

    public FrameUpdateEvent(float deltaTime)
    {
        this.DeltaTime = deltaTime;
    }

    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus _)
        => timeEventsBus.FrameUpdatePublisher.OnNext(DeltaTime);
}
