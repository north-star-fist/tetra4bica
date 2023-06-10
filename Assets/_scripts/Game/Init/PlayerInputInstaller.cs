using Tetra4bica.Input;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Tetra4bica.Init {

    /// <summary>
    /// Zenject <see cref="MonoInstaller"/> that creates <see cref="PlayerInputInstaller.PlayerInput"/>
    /// and provides it for injections.
    /// </summary>
    public partial class PlayerInputInstaller : MonoInstaller {

        public Canvas touchScreenButtonsCanvas;

        [Tooltip("UI Up button")]
        public Button screenUpButton;
        [Tooltip("UI Down button")]
        public Button screenDownButton;
        [Tooltip("UI Left button")]
        public Button screenLeftButton;
        [Tooltip("UI Right button")]
        public Button screenRightButton;

        [Tooltip("UI Shoot button")]
        public Button screenShootButton;
        [Tooltip("UI Rotate button")]
        public Button screenRotateButton;

        [Tooltip("UI Pause/Resume buttons")]
        public Button[] pauseResumeButtons;

        [Tooltip("UI Start New Game buttons")]
        public Button[] startNewGameButtons;
        [Tooltip("UI Exit buttons")]
        public Button[] exitButtons;

        public GameObject[] deactivateOnDisable;
        public GameObject[] activateOnEnable;

        /// <summary> Sends units when player pushes a button to start new game. </summary>
        Subject<Unit> _gameStartStream = new Subject<Unit>();

        /// <summary> Sends units when player pauses or resumes the game. </summary>
        Subject<Unit> _gamePauseResumeStream = new Subject<Unit>();


        public override void InstallBindings() {

            PlayerInput inputSetup = new PlayerInput(
                uiGameStartStream: _gameStartStream,
                uiPauseResumeStream: _gamePauseResumeStream,
                uiRightButtonStream: screenRightButton.OnClickAsObservable(),
                uiLeftButtonStream: screenLeftButton.OnClickAsObservable(),
                uiUpButtonStream: screenUpButton.OnClickAsObservable(),
                uiDownButtonStream: screenDownButton.OnClickAsObservable(),
                uiShootButtonStream: screenShootButton.OnClickAsObservable(),
                uiRotateButtonStream: screenRotateButton.OnClickAsObservable(),
                () => {
                    enableGameObjects(activateOnEnable, true);
                },
                () => {
                    enableGameObjects(deactivateOnDisable, false);
                }
            );
            Container.BindInstance(inputSetup).AsSingle();
            foreach (var pauseResumeButton in pauseResumeButtons) {
                pauseResumeButton.OnClickAsObservable().Subscribe(_ => _gamePauseResumeStream.OnNext(Unit.Default));
            }
            foreach (var startButton in startNewGameButtons) {
                startButton.OnClickAsObservable().Subscribe(_ => _gameStartStream.OnNext(Unit.Default));
            }
            foreach (var exitButton in exitButtons) {
                exitButton.OnClickAsObservable().Subscribe(_ => Exit());
            }
        }

        private void Awake() {
            if (!Application.isMobilePlatform) {
                // Disable touchscreen canvas for non-mobile platforms.
                touchScreenButtonsCanvas.gameObject.SetActive(false);
            }
        }

        void Exit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        private void enableGameObjects(GameObject[] gameObjects, bool enable) {
            foreach (var go in gameObjects) {
                go.SetActive(enable);
            }
        }
    }
}