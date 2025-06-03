using UniRx;
using static Tetra4bica.Input.PlayerInput;

namespace Tetra4bica.Core
{
    public interface IGameInputBus
    {

        public ISubject<MovementInput> PlayerMovementStream { get; }
        public ISubject<Unit> PlayerShotStream { get; }
        public ISubject<bool> PlayerRotateStream { get; }
        public ISubject<Unit> GameStartStream { get; }
        public ISubject<bool> GamePauseResumeStream { get; }
    }
}
