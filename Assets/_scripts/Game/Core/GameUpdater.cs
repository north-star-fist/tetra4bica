using System;
using UniRx;
using UnityEngine;

namespace Tetra4bica.Core
{

    /// <summary>
    /// This class updates the game.
    /// </summary>
    public class GameUpdater : MonoBehaviour, IGameTimeEvents
    {

        private readonly ISubject<float> _frames = new Subject<float>();

        IObservable<float> IGameTimeEvents.FrameUpdateStream => _frames;

        private bool _started;

        private void Awake()
        {
            Observable.EveryGameObjectUpdate().Subscribe(_ =>
            {
                if (_started)
                {
                    _frames.OnNext(Time.deltaTime);
                }
            });
        }

        public void StartFrames()
        {
            _started = true;
        }

        public void StopFrames()
        {
            _started = false;
        }
    }
}
