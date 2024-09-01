using Tetra4bica.Input;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Tetra4bica.Init
{

    /// <summary>
    /// Zenject <see cref="MonoInstaller"/> that creates <see cref="PlayerInputInstaller.PlayerInput"/>
    /// and provides it for injections.
    /// </summary>
    public partial class PlayerInputInstaller : MonoInstaller
    {

        [SerializeField]
        private Canvas touchScreenButtonsCanvas;

        [SerializeField, Tooltip("UI Up button")]
        private Button screenUpButton;
        [SerializeField, Tooltip("UI Down button")]
        private Button screenDownButton;
        [SerializeField, Tooltip("UI Left button")]
        private Button screenLeftButton;
        [SerializeField, Tooltip("UI Right button")]
        private Button screenRightButton;

        [SerializeField, Tooltip("UI Shoot button")]
        private Button screenShootButton;
        [SerializeField, Tooltip("UI Rotate button")]
        private Button screenRotateButton;

        [SerializeField, Tooltip("UI Pause/Resume buttons")]
        private Button[] pauseResumeButtons;

        [SerializeField, Tooltip("UI Start New Game buttons")]
        private Button[] startNewGameButtons;
        [SerializeField, Tooltip("UI Exit buttons")]
        private Button[] exitButtons;

        [SerializeField]
        private GameObject[] deactivateOnDisable;
        [SerializeField]
        private GameObject[] activateOnEnable;

        /// <summary> Sends units when player pushes a button to start new game. </summary>
        readonly Subject<Unit> gameStartStream = new Subject<Unit>();

        /// <summary> Sends units when player pauses or resumes the game. </summary>
        readonly Subject<Unit> gamePauseResumeStream = new Subject<Unit>();


        public override void InstallBindings()
        {

            PlayerInput inputSetup = new PlayerInput(
                uiGameStartStream: gameStartStream,
                uiPauseResumeStream: gamePauseResumeStream,
                uiRightButtonStream: screenRightButton.OnClickAsObservable(),
                uiLeftButtonStream: screenLeftButton.OnClickAsObservable(),
                uiUpButtonStream: screenUpButton.OnClickAsObservable(),
                uiDownButtonStream: screenDownButton.OnClickAsObservable(),
                uiShootButtonStream: screenShootButton.OnClickAsObservable(),
                uiRotateButtonStream: screenRotateButton.OnClickAsObservable(),
                () => enableGameObjects(activateOnEnable, true),
                () => enableGameObjects(deactivateOnDisable, false)
            );
            Container.BindInstance(inputSetup).AsSingle();
            foreach (var pauseResumeButton in pauseResumeButtons)
            {
                pauseResumeButton.OnClickAsObservable().Subscribe(_ => gamePauseResumeStream.OnNext(Unit.Default));
            }
            foreach (var startButton in startNewGameButtons)
            {
                startButton.OnClickAsObservable().Subscribe(_ => gameStartStream.OnNext(Unit.Default));
            }
            foreach (var exitButton in exitButtons)
            {
                exitButton.OnClickAsObservable().Subscribe(_ => Exit());
            }
        }

        private void Awake()
        {
            if (!Application.isMobilePlatform)
            {
                // Disable touchscreen canvas for non-mobile platforms.
                touchScreenButtonsCanvas.gameObject.SetActive(false);
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
    }
}
