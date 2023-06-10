using UniRx;

public interface IGameTimeBus {
    ISubject<float> FrameUpdatePublisher { get; }
}
