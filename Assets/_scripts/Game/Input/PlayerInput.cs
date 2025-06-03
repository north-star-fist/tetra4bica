using System;
using UniRx;
using UnityEngine;
using static Tetra4bica.Input.PlayerInput.MovementInput;

namespace Tetra4bica.Input
{

    /// <summary>
    /// Class that encapsulates all game input and provides it as streams of input events.
    /// </summary>
    public class PlayerInput
    {

        #region Non-mobile controls
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string RotateButton = "Rotate";
        private const string ShootButton = "Fire";
        private const string PauseButton = "Cancel";
        #endregion
        #region TV controls
        private const KeyCode KEY_MENU = KeyCode.Menu;
        #endregion

        private readonly Action _onEnable;
        private readonly Action _onDisable;

        /// <summary> Player tetromino movements (up/down or left/right). </summary>
        public IObservable<MovementInput> PlayerMovementStream => _playerMovementStream;
        private readonly IObservable<MovementInput> _playerMovementStream;

        /// <summary>
        /// Player tetramino rotatations clockwise (true) or anticlockwise (false).
        /// </summary>
        public IObservable<bool> PlayerRotateStream => _playerRotateStream;
        private readonly IObservable<bool> _playerRotateStream;

        public IObservable<Unit> PlayerShotStream => _playerShotStream;
        private readonly IObservable<Unit> _playerShotStream;

        /// <summary> Sends units when player pushes a button to start new game. </summary>
        public IObservable<Unit> GameStartStream => _gameStartStream;
        private readonly IObservable<Unit> _gameStartStream;

        /// <summary> Sends true when player pauses the game and false when they resumes it. </summary>
        public IObservable<bool> GamePauseResumeStream => _gamePauseResumeStream;
        private readonly IObservable<bool> _gamePauseResumeStream;

        private bool _stopped = true;

        public PlayerInput(
            IObservable<Unit> uiGameStartStream,
            IObservable<Unit> uiPauseResumeStream,
            IObservable<Unit> uiRightButtonStream,
            IObservable<Unit> uiLeftButtonStream,
            IObservable<Unit> uiUpButtonStream,
            IObservable<Unit> uiDownButtonStream,
            IObservable<Unit> uiShootButtonStream,
            IObservable<Unit> uiRotateButtonStream,
            Action onEnable,
            Action onDisable
        )
        {
            _onEnable = onEnable;
            _onDisable = onDisable;

            _playerMovementStream = combineMovementEventSources(
                uiRightButtonStream,
                uiLeftButtonStream,
                uiUpButtonStream,
                uiDownButtonStream
            );

            _playerRotateStream = createRotationsStream(uiRotateButtonStream);
            _playerShotStream = createShotsStream(uiShootButtonStream);

            _gameStartStream = uiGameStartStream;
            _gamePauseResumeStream = combineAllPauseResumeSources(uiPauseResumeStream);
        }


        /// <summary>
        /// Starts processing of user input.
        /// Use this method when the gameplay is ready to recieve player's input.
        /// To stop/pause processing of  player's input use <see cref="Disable"/>
        /// </summary>
        public void Enable()
        {
            _stopped = false;
            _onEnable();
        }

        /// <summary>
        /// Stops processing of user input.
        /// Use this method when the gameplay is paused or there is no any gameplay at all.
        /// To start processing player's input again use <see cref="Enable"/>
        /// </summary>
        public void Disable()
        {
            _stopped = true;
            _onDisable();
        }

        private IObservable<MovementInput> combineMovementEventSources(
            IObservable<Unit> uiRightButtonStream,
            IObservable<Unit> uiLeftButtonStream,
            IObservable<Unit> uiUpButtonStream,
            IObservable<Unit> uiDownButtonStream
        )
        {
            IObservable<MovementInput> screenButtonsHorizontalInputStream =
                uiRightButtonStream.Select(_ => new MovementInput(HorizontalInput.Right))
                .Merge(new[] { uiLeftButtonStream.Select(_ => new MovementInput(HorizontalInput.Left)) });
            IObservable<MovementInput> screenButtonsVerticalInputStream =
                uiUpButtonStream.Select(_ => new MovementInput(VerticalInput.Up))
                .Merge(new[] { uiDownButtonStream.Select(_ => new MovementInput(VerticalInput.Down)) });

            IObservable<HorizontalInput> playerHorizontalInputStream = Observable.EveryGameObjectUpdate()
                .Select(_ => !_stopped && UnityEngine.Input.GetButtonDown(HorizontalAxis))
                .Select(pressed => pressed
                    ? UnityEngine.Input.GetAxis(HorizontalAxis) > 0
                        ? HorizontalInput.Right
                        : HorizontalInput.Left
                    : HorizontalInput.None);
            IObservable<VerticalInput> playerVerticalInputStream = Observable.EveryGameObjectUpdate()
                .Select(_ => !_stopped && UnityEngine.Input.GetButtonDown(VerticalAxis))
                .Select(pressed => pressed
                    ? UnityEngine.Input.GetAxis(VerticalAxis) > 0
                        ? VerticalInput.Up
                        : VerticalInput.Down
                    : VerticalInput.None);
            // Non screen input streams are updated each frame so can be
            // easily zipped for simultaneous pressing horizontal and vertical buttons.
            var combinedPlayerMovementStream = playerHorizontalInputStream
                .Zip(playerVerticalInputStream, (hor, ver) => new MovementInput(hor, ver))
                .Where(inp => inp.Vertical != VerticalInput.None || inp.Horizontal != HorizontalInput.None)
                // Merging with screen button events
                .Merge(screenButtonsHorizontalInputStream, screenButtonsVerticalInputStream);
            return combinedPlayerMovementStream;
        }

        private IObservable<Unit> createShotsStream(IObservable<Unit> uiShootButtonStream)
        {
            var shotStream = uiShootButtonStream;
            if (!Application.isMobilePlatform)
            {
                shotStream = shotStream.Merge(new[] {
                    // Getting shots input from non-touchscreen sources only if the platform is not mobile to prevent shots 
                    // on any screen touches (keeping it only on shot screen button pushed)
                    Observable.EveryGameObjectUpdate().Where(_ => !_stopped && UnityEngine.Input.GetButtonDown(ShootButton))
                    .Select(_ => Unit.Default)
                });
            }

            return shotStream;
        }

        private IObservable<bool> createRotationsStream(IObservable<Unit> uiRotateButtonStream)
        {
            // Rotate clockwise always so emit true only.
            var rotateStream = uiRotateButtonStream.Select(_ => true);
            if (!Application.isMobilePlatform)
            {
                // Getting rotates input from non-touchscreen sources only if the platform is not mobile to prevent handling
                // screen touches (keeping it only on rotate screen button pushed)
                rotateStream = rotateStream.Merge(new[] {
                    Observable.EveryGameObjectUpdate().Where(_ => !_stopped && UnityEngine.Input.GetButtonDown(RotateButton))
                    .Select(_ => true)
                });
            }
            return rotateStream;
        }

        private static IObservable<bool> combineAllPauseResumeSources(IObservable<Unit> uiPauseResumeStream)
        {
            return Observable.EveryGameObjectUpdate()
                .Where(_ =>
                    UnityEngine.Input.GetButtonDown(PauseButton)
                    || UnityEngine.Input.GetKeyDown(KEY_MENU)
                ).Select(_ => Unit.Default).Merge(new[] { uiPauseResumeStream })
                .Select(_ => true).Scan(false, (paused, _) => !paused);
        }

        [Serializable]
        public readonly struct MovementInput
        {
            [Serializable]
            public enum HorizontalInput { None = 0, Left = 1, Right = 2 };
            [Serializable]
            public enum VerticalInput { None = 0, Up = 1, Down = 2 };

            public HorizontalInput Horizontal => _horizontal;
            private readonly HorizontalInput _horizontal;
            public VerticalInput Vertical => _vertical;
            private readonly VerticalInput _vertical;

            public MovementInput(HorizontalInput hor, VerticalInput ver)
            {
                _horizontal = hor;
                _vertical = ver;
            }

            public MovementInput(HorizontalInput hor) : this(hor, VerticalInput.None)
            {
            }

            public MovementInput(VerticalInput ver) : this(HorizontalInput.None, ver)
            {
            }
        }
    }
}
