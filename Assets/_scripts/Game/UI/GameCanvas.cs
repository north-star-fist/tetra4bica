using System.Collections.Generic;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.UI {

    /// <summary> Canvas reacting on game state. </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameCanvas : MonoBehaviour {
        [Inject]
        IGameEvents gameLogic;

        public PhasePredicate[] gamePhases;

        Canvas canvas;

        Dictionary<GamePhase, bool> phaseMap = new Dictionary<GamePhase, bool>();

        private void Awake() {

            foreach (var phase in gamePhases) {
                phaseMap.Add(phase.phase, phase.enabled);
            }

            canvas = GetComponent<Canvas>();

            gameLogic.GamePhaseStream.Subscribe(
                p => {
                    canvas.enabled = phaseMap.TryGetValue(p, out var enabled) && enabled;
                }
            );
        }
    }
}