using System;
using System.Collections.Generic;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;


namespace Tetra4bica.UI
{

    /// <summary> Canvas reacting on game state. </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameCanvas : MonoBehaviour, IDisposable
    {

        [SerializeField, FormerlySerializedAs("gamePhases")]
        private PhasePredicate[] _gamePhases;

        Canvas _canvas;
        readonly Dictionary<GamePhase, bool> _phaseMap = new Dictionary<GamePhase, bool>();

        private IDisposable _gamePhaseSubscription;
        private bool _disposedValue;

        private void Awake()
        {
            foreach (var phase in _gamePhases)
            {
                _phaseMap.Add(phase.Phase, phase.Enabled);
            }
            
            _canvas = GetComponent<Canvas>();
        }

        public void Setup(IObservable<GamePhase> gamePhaseEvents)
        {
            if (_gamePhaseSubscription != null)
            {
                _gamePhaseSubscription.Dispose();
            }

            _gamePhaseSubscription = gamePhaseEvents.Subscribe(
                gp =>
                {
                    if (_canvas != null)
                    {
                        _canvas.enabled = _phaseMap.TryGetValue(gp, out var enabled) && enabled;
                    }
                });
        }

        #region Disposable pattern
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_gamePhaseSubscription != null)
                    {
                        _gamePhaseSubscription.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }
        #endregion

    }
}
