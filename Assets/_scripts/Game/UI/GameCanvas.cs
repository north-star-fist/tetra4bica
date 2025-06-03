using System.Collections.Generic;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Tetra4bica.UI
{

    /// <summary> Canvas reacting on game state. </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameCanvas : MonoBehaviour
    {
        [Inject]
        IGameEvents _gameLogic;

        [SerializeField, FormerlySerializedAs("gamePhases")]
        private PhasePredicate[] _gamePhases;

        Canvas _canvas;

        readonly Dictionary<GamePhase, bool> _phaseMap = new Dictionary<GamePhase, bool>();

        private void Awake()
        {

            foreach (var phase in _gamePhases)
            {
                _phaseMap.Add(phase.Phase, phase.Enabled);
            }

            _canvas = GetComponent<Canvas>();

            _gameLogic.GamePhaseStream.Subscribe(
                p =>
                {
                    _canvas.enabled = _phaseMap.TryGetValue(p, out var enabled) && enabled;
                }
            );
        }
    }
}
