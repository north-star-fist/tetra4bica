using System;

namespace Tetra4bica.Core
{
    public interface IGameTimeEvents
    {

        /// <summary> Updates game logic once per frame specifying the time passed since previous Update. </summary>
        public IObservable<float> FrameUpdateStream { get; }

        public void StartFrames();

        public void StopFrames();
    }
}
