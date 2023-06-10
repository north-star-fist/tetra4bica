using System;

[Serializable]
public class PauseResumeEvent : IGameInputEvent {

    private readonly bool pause;

    public PauseResumeEvent(bool pause) {
        this.pause = pause;
    }

    public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
        => inputBus.GamePauseResumeStream.OnNext(pause);
}
