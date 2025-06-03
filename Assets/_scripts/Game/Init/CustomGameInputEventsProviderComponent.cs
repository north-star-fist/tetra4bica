using System;
using Tetra4bica.Core;
using UnityEngine;

namespace Tetra4bica.Init
{
    public class CustomGameInputEventsProviderComponent : MonoBehaviour, IGameInputEventProvider
    {
        virtual public IObservable<IGameInputEvent> GetInputStream()
        {
            throw new NotImplementedException();
        }
    }
}
