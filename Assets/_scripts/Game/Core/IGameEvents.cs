using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetra4bica.Core {

    /// <summary>
    /// Interface providing all output game events via particular <see cref="IObservable{T}"/>s.
    /// </summary>
    public interface IGameEvents {

        /// <summary>
        /// Stream notifying about new game strarting with particular game field size as payload.
        /// </summary>
        public IObservable<Vector2Int> GameStartedStream { get; }

        /// <summary> Stream of the level color table patches. </summary>
        public IObservable<Cell> NewCellStream { get; }

        /// <summary> Stream of the level color table scrolls left. Has new cells wall as payload. </summary>
        public IObservable<IEnumerable<CellColor>> TableScrollStream { get; }

        /// <summary> Player's tetramino stream. </summary>
        public IObservable<PlayerTetromino> PlayerTetrominoStream { get; }

        /// <summary> Stream of shots bringing shot direction as data. </summary>
        public IObservable<Vector2Int> ShotStream { get; }

        /// <summary> Stream of shots bringing shot direction as data. </summary>
        public IObservable<bool> RotationStream { get; }

        /// <summary>
        /// Stream of exploded game cells. Delivers eliminated cell coordinates and color.
        /// </summary>
        public IObservable<Cell> EliminatedBricksStream { get; }

        /// <summary>
        /// Stream bringing updates about projectiles. Should be used coupled with <see cref="GameFrameStream"/>
        /// that indicates that all previously received particles (in previous frame) are outdated
        /// (and can be vanished from the screen for example).
        /// </summary>
        public IObservable<Vector2> ProjectileCoordinatesStream { get; }

        /// <summary>
        /// Stream that informs listeners that somewhere projectile was frozen into game call.
        /// </summary>
        public IObservable<Vector2Int> FrozenProjectilesStream { get; }

        /// <summary>
        /// Stream notifying listeners about current player's scores.
        /// </summary>
        public IObservable<uint> ScoreStream { get; }

        /// <summary> Stream that deliver game phase switches. </summary>
        public IObservable<GamePhase> GamePhaseStream { get; }

        /// <summary>
        /// Frame stream with time from the last frame as payload.
        /// Each frame means all alive particles refresh.
        /// All particles delivered by <see cref="ProjectileCoordinatesStream"/> after frame refresh 
        /// should be treated as particles alive only at rhe current frame. 
        /// </summary>
        public IObservable<float> FrameUpdateStream { get; }

    }
}