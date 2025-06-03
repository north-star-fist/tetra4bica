using System;

namespace Tetra4bica.Core
{
    [Serializable]
    public class PauseResumeEvent : IGameInputEvent
    {

        private readonly bool _pause;

        public PauseResumeEvent(bool pause)
        {
            this._pause = pause;
        }

        public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus)
            => inputBus.GamePauseResumeStream.OnNext(_pause);
    }
}
