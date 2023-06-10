using System;
using Tetra4bica.Core;
using Tetra4bica.Input;
using UniRx;
using Zenject;

[ZenjectAllowDuringValidation]
public class DefaultGameInputEventProvider : IGameInputEventProvider {

    [Inject]
    PlayerInput playerInput;

    [Inject]
    IGameTimeEvents gameTimeEventsBus;

    public IObservable<IGameInputEvent> GetInputStream() {
        return gameTimeEventsBus.FrameUpdateStream
            .Select<float, IGameInputEvent>(dT => new FrameUpdateEvent(dT))
            .Merge(new IObservable<IGameInputEvent>[] {
                    playerInput.playerMovementStream.Select(m => new MotionEvent(m)),
                    playerInput.playerShotStream.Select(_ => new ShotEvent()),
                    playerInput.playerRotateStream.Select(cw => new RotateEvent(cw)),
                    playerInput.gameStartStream.Select(_ => new StartNewGameEvent()),
                    playerInput.gamePauseResumeStream.Select(p => new PauseResumeEvent(p))
            });
    }
}
