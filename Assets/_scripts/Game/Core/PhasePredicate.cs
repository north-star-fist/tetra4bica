using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tetra4bica.Core
{

    [Serializable]
    public class PhasePredicate
    {
        [SerializeField, FormerlySerializedAs("phase")]
        private GamePhase _phase;
        [SerializeField, FormerlySerializedAs("enabled")]
        private bool _enabled;

        public GamePhase Phase => _phase;
        public bool Enabled => _enabled;
    }
}
