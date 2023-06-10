using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Core {

    public class GamePhaseObjectActivator : MonoBehaviour {
        public GameObject[] objects;

        [Inject]
        IGameEvents gameEvents;

        public PhasePredicate[] gamePhases;

        Dictionary<GamePhase, bool> phaseMap = new Dictionary<GamePhase, bool>();

        private void Awake() {

            foreach (var phase in gamePhases) {
                phaseMap.Add(phase.phase, phase.enabled);
            }

            gameEvents.GamePhaseStream.Subscribe(
                phase => {
                    foreach (var obj in objects) {
                        obj.SetActive(phaseMap.TryGetValue(phase, out var enabled) && enabled);
                    }
                }
            );
        }
    }
}