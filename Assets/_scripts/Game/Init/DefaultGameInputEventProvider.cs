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
        PlayerInput playerInput;

        [Inject]
        IGameTimeEvents gameTimeEventsBus;

        public IObservable<IGameInputEvent> GetInputStream()
        {
            return gameTimeEventsBus.FrameUpdateStream
                .Select<float, IGameInputEvent>(dT => new FrameUpdateEvent(dT))
                .Merge(new IObservable<IGameInputEvent>[] {
                    playerInput.PlayerMovementStream.Select(m => new MotionEvent(m)),
                    playerInput.PlayerShotStream.Select(_ => new ShotEvent()),
                    playerInput.PlayerRotateStream.Select(cw => new RotateEvent(cw)),
                    playerInput.GameStartStream.Select(_ => new StartNewGameEvent()),
                    playerInput.GamePauseResumeStream.Select(p => new PauseResumeEvent(p))
                });
        }
    }
}
