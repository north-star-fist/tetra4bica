using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.Core
{

    public class GamePhaseObjectActivator : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("objects")]
        private GameObject[] _objects;
        [SerializeField, FormerlySerializedAs("gamePhases")]
        private PhasePredicate[] _gamePhases;

        [Inject]
        private IGameEvents _gameEvents;

        private readonly Dictionary<GamePhase, bool> _phaseMap = new Dictionary<GamePhase, bool>();

        private void Awake()
        {

            foreach (var phase in _gamePhases)
            {
                _phaseMap.Add(phase.Phase, phase.Enabled);
            }

            _gameEvents.GamePhaseStream.Subscribe(
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
