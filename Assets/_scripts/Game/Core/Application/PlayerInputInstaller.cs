using Tetra4bica.Core;
using Tetra4bica.Input;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;


namespace Tetra4bica.Init
{

    /// <summary>
    /// Installer that creates <see cref="PlayerInputInstaller.PlayerInput"/>
    /// and provides it for injections.
    /// </summary>
    public class PlayerInputInstaller : LifetimeScope
    {

        [SerializeField, FormerlySerializedAs("touchScreenButtonsCanvas")]
        private Canvas _touchScreenButtonsCanvas;
        [Header("UI D-Pad")]
        [SerializeField, Tooltip("UI Up button"), FormerlySerializedAs("screenUpButton")]
        private Button _screenUpButton;
        [SerializeField, Tooltip("UI Down button"), FormerlySerializedAs("screenDownButton")]
        private Button _screenDownButton;
        [SerializeField, Tooltip("UI Left button"), FormerlySerializedAs("screenLeftButton")]
        private Button _screenLeftButton;
        [SerializeField, Tooltip("UI Right button"), FormerlySerializedAs("screenRightButton")]
        private Button _screenRightButton;

        [Header("UI A, B buttons")]
        [SerializeField, Tooltip("UI Shoot button"), FormerlySerializedAs("screenShootButton")]
        private Button _screenShootButton;
        [SerializeField, Tooltip("UI Rotate button"), FormerlySerializedAs("screenRotateButton")]
        private Button _screenRotateButton;

        [Header("Other UI buttons")]
        [SerializeField, Tooltip("UI Pause/Resume buttons"), FormerlySerializedAs("pauseResumeButtons")]
        private Button[] _pauseResumeButtons;
        [SerializeField, Tooltip("UI Start New Game buttons"), FormerlySerializedAs("startNewGameButtons")]
        private Button[] _startNewGameButtons;
        [SerializeField, Tooltip("UI Exit buttons"), FormerlySerializedAs("exitButtons")]
        private Button[] _exitButtons;

        [Header("Objects to trigger")]
        [SerializeField, FormerlySerializedAs("deactivateOnDisable")]
        private GameObject[] _deactivateOnDisable;
        [SerializeField, FormerlySerializedAs("activateOnEnable")]
        private GameObject[] _activateOnEnable;
        private PlayerInput _inputSetup;

        /// <summary> Sends units when player pushes a button to start new game. </summary>
        readonly Subject<Unit> _gameStartStream = new Subject<Unit>();

        /// <summary> Sends units when player pauses or resumes the game. </summary>
        readonly Subject<Unit> _gamePauseResumeStream = new Subject<Unit>();

        public PlayerInput PlayerInput
        {
            get
            {
                _inputSetup ??= createPlayerInput();
                return _inputSetup;
            }
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            _inputSetup = createPlayerInput();
            //Container.BindInstance(inputSetup).AsSingle();
            builder.RegisterInstance(_inputSetup).As<PlayerInput>();

            foreach (var pauseResumeButton in _pauseResumeButtons)
            {
                pauseResumeButton.OnClickAsObservable().Subscribe(_ => _gamePauseResumeStream.OnNext(Unit.Default));
            }
            foreach (var startButton in _startNewGameButtons)
            {
                startButton.OnClickAsObservable().Subscribe(_ =>
                {
                    _gameStartStream.OnNext(Unit.Default);
                });
            }
            foreach (var exitButton in _exitButtons)
            {
                exitButton.OnClickAsObservable().Subscribe(_ => Exit());
            }

            /*
            builder.RegisterBuildCallback(container =>
            {
                var gameLogic = container.Resolve<IGameEvents>();
                gameLogic.GamePhaseStream.Subscribe(phase =>
                {
                    if (phase is GamePhase.Started)
                    {
                        _inputSetup.Enable();
                    }
                    else
                    {
                        _inputSetup.Disable();
                    }
                });
                // Do not process player input too early. Let game to start first.
                _inputSetup.Disable();
            });
            */
        }


        protected override void Awake()
        {
            base.Awake();
            if (!Application.isMobilePlatform)
            {
                // Disable touchscreen canvas for non-mobile platforms.
                _touchScreenButtonsCanvas.gameObject.SetActive(false);
            }
        }

        void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        private void enableGameObjects(GameObject[] gameObjects, bool enable)
        {
            foreach (var go in gameObjects)
            {
                go.SetActive(enable);
            }
        }

        private PlayerInput createPlayerInput() => new PlayerInput(
            uiGameStartStream: _gameStartStream,
            uiPauseResumeStream: _gamePauseResumeStream,
            uiRightButtonStream: _screenRightButton.OnClickAsObservable(),
            uiLeftButtonStream: _screenLeftButton.OnClickAsObservable(),
            uiUpButtonStream: _screenUpButton.OnClickAsObservable(),
            uiDownButtonStream: _screenDownButton.OnClickAsObservable(),
            uiShootButtonStream: _screenShootButton.OnClickAsObservable(),
            uiRotateButtonStream: _screenRotateButton.OnClickAsObservable(),
            () => enableGameObjects(_activateOnEnable, true),
            () => enableGameObjects(_deactivateOnDisable, false)
        );
    }
}
