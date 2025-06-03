using System;
using Tetra4bica.Core;
using Tetra4bica.Input;
using UniRx;
using Zenject;

namespace Tetra4bica.Init
{
    [ZenjectAllowDuringValidation]
    public class DefaultGameInputEventProvider : IGameInputEventProvider
    {

        [Inject]
        private PlayerInput _playerInput;

        [Inject]
        private IGameTimeEvents _gameTimeEventsBus;

        public IObservable<IGameInputEvent> GetInputStream()
        {
            return _gameTimeEventsBus.FrameUpdateStream
                .Select<float, IGameInputEvent>(dT => new FrameUpdateEvent(dT))
                .Merge(new IObservable<IGameInputEvent>[] {
                    _playerInput.PlayerMovementStream.Select(m => new MotionEvent(m)),
                    _playerInput.PlayerShotStream.Select(_ => new ShotEvent()),
                    _playerInput.PlayerRotateStream.Select(cw => new RotateEvent(cw)),
                    _playerInput.GameStartStream.Select(_ => new StartNewGameEvent()),
                    _playerInput.GamePauseResumeStream.Select(p => new PauseResumeEvent(p))
                });
        }
    }
}
