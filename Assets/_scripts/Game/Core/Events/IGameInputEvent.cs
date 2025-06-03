/// <summary>
/// Game Input Event interface for any game input including frame updates.
/// Implemented as command pattern. Used for recording game input events and playing them back for debugging reasons.
/// </summary>

namespace Tetra4bica.Core
{
    public interface IGameInputEvent
    {
        public void Apply(IGameTimeBus timeEventsBus, IGameInputBus inputBus);
    }
}
