using System;
using UnityEngine;

namespace Tetra4bica.Core
{

    [Serializable]
    public class PhasePredicate
    {
        public GamePhase Phase => phase;
        public bool Enabled => enabled;
        [SerializeField]
        private GamePhase phase;
        [SerializeField]
        private bool enabled;
    }
}
