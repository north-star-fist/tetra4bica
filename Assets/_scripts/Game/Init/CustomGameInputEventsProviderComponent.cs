using System;
using UnityEngine;

public class CustomGameInputEventsProviderComponent : MonoBehaviour, IGameInputEventProvider {
    virtual public IObservable<IGameInputEvent> GetInputStream() {
        throw new NotImplementedException();
    }
}
