using System;
using Tetra4bica.Input;
using static Tetra4bica.Input.PlayerInput;

namespace Tetra4bica.Core
{
    [Serializable]
    public class MotionEvent : IGameInputEvent
    {

        private readonly PlayerInput.MovementInput _motion;

        public MotionEvent(MovementInput motion)
        {
            _motion = motion;
        }

        public void Apply(IGameTimeBus _, IGameInputBus inputBus)
            => inputBus.PlayerMovementStream.OnNext(_motion);
    }
}
