using System;

/// <summary> Game Input Events provider interface. </summary>

namespace Tetra4bica.Core
{
    public interface IGameInputEventProvider
    {
        public IObservable<IGameInputEvent> GetInputStream();
    }
}
