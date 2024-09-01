using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Core
{

    public class GamePhaseObjectActivator : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] objects;
        [SerializeField]
        private PhasePredicate[] gamePhases;

        [Inject]
        IGameEvents gameEvents;

        Dictionary<GamePhase, bool> phaseMap = new Dictionary<GamePhase, bool>();

        private void Awake()
        {

            foreach (var phase in gamePhases)
            {
                phaseMap.Add(phase.Phase, phase.Enabled);
            }

            gameEvents.GamePhaseStream.Subscribe(
                phase =>
                {
                    foreach (var obj in objects)
                    {
                        obj.SetActive(phaseMap.TryGetValue(phase, out var enabled) && enabled);
                    }
                }
            );
        }
    }
}
