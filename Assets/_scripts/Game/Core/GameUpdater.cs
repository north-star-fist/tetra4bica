using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Core
{

    /// <summary>
    /// This class updates the game.
    /// </summary>
    [ZenjectAllowDuringValidation]
    public class GameUpdater : MonoBehaviour, IGameTimeEvents
    {

        private readonly ISubject<float> frames = new Subject<float>();

        IObservable<float> IGameTimeEvents.FrameUpdateStream => frames;

        private void Awake()
        {
            Observable.EveryGameObjectUpdate().Subscribe(_ => frames.OnNext(Time.deltaTime));
        }

    }
}
