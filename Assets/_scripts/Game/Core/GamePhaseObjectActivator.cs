using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;


namespace Tetra4bica.Core
{

    public class GamePhaseObjectActivator : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("objects")]
        private GameObject[] _objects;
        [SerializeField, FormerlySerializedAs("gamePhases")]
        private PhasePredicate[] _gamePhases;

        private readonly Dictionary<GamePhase, bool> _phaseMap = new Dictionary<GamePhase, bool>();

        IDisposable _subscription;

        private void Awake()
        {
            foreach (var phase in _gamePhases)
            {
                _phaseMap.Add(phase.Phase, phase.Enabled);
            }
        }

        public void Setup(IObservable<GamePhase> gamePhaseStream)
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
            }
            _subscription = gamePhaseStream.Subscribe(
                phase =>
                {
                    foreach (var obj in _objects)
                    {
                        obj.SetActive(_phaseMap.TryGetValue(phase, out var enabled) && enabled);
                    }
                }
            );
        }
    }
}
