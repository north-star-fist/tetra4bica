using System;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;

namespace Tetra4bica.Core
{
    public interface IScoreUi
    {
        public void Setup(
            IObservable<Vector2Int> gameStartedStream,
            IObservable<Cell> eliminatedBricksObservable,
            IObservable<uint> scoresObservable,
            IObservable<Unit> gameOverObservable,

            IVisualSettings visualSettings,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager
        );
    }
}
