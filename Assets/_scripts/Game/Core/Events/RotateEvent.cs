using System;

namespace Tetra4bica.Core
{
    [Serializable]
    public class RotateEvent : IGameInputEvent
    {

        private readonly bool _clockwise;

        public RotateEvent(bool clockwise)
        {
            this._clockwise = clockwise;
        }

        public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
            => inputBus.PlayerRotateStream.OnNext(_clockwise);
    }
}
