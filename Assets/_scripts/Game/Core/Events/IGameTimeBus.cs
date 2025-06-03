using UniRx;

namespace Tetra4bica.Core
{
    public interface IGameTimeBus
    {
        ISubject<float> FrameUpdatePublisher { get; }
    }
}
