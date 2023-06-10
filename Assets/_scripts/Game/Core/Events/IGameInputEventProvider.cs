using System;

/// <summary> Game Input Events provider interface. </summary>
public interface IGameInputEventProvider {
    public IObservable<IGameInputEvent> GetInputStream();
}
