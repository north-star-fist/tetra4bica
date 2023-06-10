using System;
using Tetra4bica.Input;
using static Tetra4bica.Input.PlayerInput;

[Serializable]
public class MotionEvent : IGameInputEvent {

    private readonly PlayerInput.MovementInput motion;

    public MotionEvent(MovementInput motion) {
        this.motion = motion;
    }

    public void Apply(IGameTimeBus _, IGameInputBus inputBus)
        => inputBus.PlayerMovementStream.OnNext(motion);
}
