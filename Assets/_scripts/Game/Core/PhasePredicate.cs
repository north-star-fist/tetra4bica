using System;
using UnityEngine;

namespace Tetra4bica.Core
{

    [Serializable]
    public class PhasePredicate
    {
        [SerializeField]
        private GamePhase _phase;
        [SerializeField]
        private bool _enabled;

        public GamePhase Phase => _phase;
        public bool Enabled => _enabled;
    }
}
