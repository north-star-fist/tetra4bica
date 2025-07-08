using System;
using System.Collections.Generic;
using System.Threading;
using Sergei.Safonov.Unity;
using Sergei.Safonov.StateMachinery;
using Sergei.Safonov.Unity.SceneManagement;
using Tetra4bica.Core;
using Tetra4bica.Graphics;
using Tetra4bica.Init;
using Tetra4bica.Input;
using Tetra4bica.UI;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tetra4bica.Flow
{

    public class AppStateGame : IState
    {
        private const string SceneKeyGameplay = "Gameplay";

        private readonly ISceneManager _sceneManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IGameObjectPoolManager _poolManager;
        private readonly GameLogic.GameSettings _gameSettings;
        private readonly ICellPatterns _tetraminoPatterns;
        private readonly ICellGenerator _cellGenerator;
        private readonly IGameTimeEvents _timeEvents;
        private readonly IAudioSourceManager _audioManager;

        private IColorTableView _gameTable;
        private IHudUi _uiManager;

        private PlayerInput _inputProvider;

        private readonly ReactiveProperty<int> _toCollectLeft = new();

        private int _score;
        private GameLogic _gameLogic;
        private readonly List<GameCanvas> _gameCanvasBuffer = new();
        private readonly List<GamePhaseObjectActivator> _objectActivatorBuffer = new();
        

        private CompositeDisposable _disposables = new CompositeDisposable();
        

        public string StateId => nameof(AppStateGame);

        public AppStateGame(
            ISceneManager sceneManager,
            ISettingsManager settingsManager,
            IGameObjectPoolManager poolManager,
            IAudioSourceManager audioManager,
            GameLogic.GameSettings gameSettings,
            ICellPatterns tetraminoPatterns,
            ICellGenerator cellGenerator,
            IGameTimeEvents timeEvents
        )
        {
            // utilities
            _sceneManager = sceneManager;
            _settingsManager = settingsManager;
            _poolManager = poolManager;
            _audioManager = audioManager;

            // game
            _gameSettings = gameSettings;
            _tetraminoPatterns = tetraminoPatterns;
            _cellGenerator = cellGenerator;
            _timeEvents = timeEvents;
        }


        public async Awaitable OnStateEnterAsync()
        {
            _disposables.Dispose();
            _disposables = new();

            var lResult = await _sceneManager.LoadAsync("Game");
            //var gResult = await _sceneManager.LoadAsync(SceneKeyGameplay);

            if (!lResult.loadedSuccessfully || !lResult.scene.TryGetRootGameObject<IColorTableView>(out _gameTable))
            {
                throw new InvalidOperationException(
                    $"Level was not loaded! Maybe there is no {nameof(IColorTableView)} object."
                );
            }
            /*
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<IHudUi>(out _uiManager)
                )
            {
                throw new InvalidOperationException(
                    $"Game manager scene was not loaded! Maybe there is no {nameof(IHudUi)} object."
                );
            }
            */
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<PlayerInputInstaller>(out var inputInstaller)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(PlayerInputInstaller)} object."
                );
            }

            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<ProjectileVisuals>(out var projVisuals)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(ProjectileVisuals)} object."
                );
            }
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<IScoreUi>(out var scoreUi)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(IScoreUi)} object."
                );
            }
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<IPlayerVisuals>(out var playerVisuals)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(IPlayerVisuals)} object."
                );
            }
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<IVisualSettingsInstaller>(out var visualSettingsInstaller)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(IVisualSettingsInstaller)} object."
                );
            }
            if (!lResult.loadedSuccessfully
                || !lResult.scene.TryGetRootGameObject<IMainGameEventsSfx>(out var mainEventsSfx)
            )
            {
                throw new InvalidOperationException(
                    $"Game scene was not loaded! Maybe there is no {nameof(IMainGameEventsSfx)} object."
                );
            }

            _inputProvider = inputInstaller.PlayerInput;
            IGameInputEventProvider inputEventProvider = new GameInputProvider(_inputProvider, _timeEvents);
            _gameLogic = new GameLogic(_gameSettings, inputEventProvider, _tetraminoPatterns, _cellGenerator);
            _gameTable.Setup(_gameLogic, visualSettingsInstaller.GetVisualSettings(), _poolManager, _audioManager);
            projVisuals.Setup(_gameLogic, _gameLogic.FrozenProjectilesStream, _audioManager);
            scoreUi.Setup(
                _gameLogic.GameStartedStream,
                _gameLogic.EliminatedBricksStream,
                _gameLogic.ScoreStream,
                _gameLogic.GamePhaseStream.Where(g => g == GamePhase.GameOver).AsUnitObservable(),

                visualSettingsInstaller.GetVisualSettings(),
                _poolManager,
                _audioManager
            );
            playerVisuals.Setup(
                _gameLogic,
                _gameSettings,
                visualSettingsInstaller.GetVisualSettings(),
                _poolManager,
                _audioManager
            );
            mainEventsSfx.Setup(
                _gameLogic.GamePhaseStream.Scan(
                    // Getting switching to Started phase only after NotStarted or GameOver phases
                    // keeping in mind that the Game can not be started at Paused state (if it can -
                    // game starting sound is played)
                    (GamePhase.NotStarted, GamePhase.NotStarted),
                    (phaseSwitch, newPhase) =>
                    {
                        return (phaseSwitch.Item2, newPhase);
                    }
                ).Where(phaseSwitch => phaseSwitch.Item1 is GamePhase.GameOver or GamePhase.NotStarted
                    && phaseSwitch.Item2 is GamePhase.Started).Select(_ => Unit.Default),
                _gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default),
                _audioManager
            );

            initializeGameCanvases(lResult.scene);
            initializeObjectActivators(lResult.scene);

            Time.timeScale = 1f;
            _timeEvents.StartFrames();
            _inputProvider.Enable();

            return;

            void initializeGameCanvases(Scene gameScene)
            {
                _gameCanvasBuffer.Clear();
                gameScene.GetRootGameObjects(_gameCanvasBuffer);
                foreach (var gameCanvas in _gameCanvasBuffer)
                {
                    gameCanvas.Setup(_gameLogic.GamePhaseStream);
                }
            }

            void initializeObjectActivators(Scene gameScene)
            {
                _objectActivatorBuffer.Clear();
                gameScene.GetRootComponents(_objectActivatorBuffer);
                
                foreach (var objActivator in _objectActivatorBuffer)
                {
                    objActivator.Setup(_gameLogic.GamePhaseStream);
                }
            }
        }

        private void Restart()
        {
            _score = 0;
        }


        private void HandleItemCollecting(int ballData)
        {
            _score += ballData;
        }

        public async Awaitable OnStateExitAsync()
        {
            _inputProvider.Disable();
            _timeEvents.StopFrames();
            await _sceneManager.UnloadAsync(SceneKeyGameplay);
            _disposables.Dispose();
        }

        public async Awaitable<Type> StartAsync(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await Awaitable.NextFrameAsync();
            }
            return null;
        }

        private class GameInputProvider : IGameInputEventProvider
        {
            private PlayerInput _playerInput;

            private IGameTimeEvents _gameTimeEvents;

            public GameInputProvider(PlayerInput input, IGameTimeEvents timeEvents)
            {
                _playerInput = input;
                _gameTimeEvents = timeEvents;
            }

            public IObservable<IGameInputEvent> GetInputStream()
            {
                return _gameTimeEvents.FrameUpdateStream
                    .Select<float, IGameInputEvent>(dT => new FrameUpdateEvent(dT))
                    .Merge(new IObservable<IGameInputEvent>[] {
                        _playerInput.PlayerMovementStream.Select(m => new MotionEvent(m)),
                        _playerInput.PlayerShotStream.Select(_ => new ShotEvent()),
                        _playerInput.PlayerRotateStream.Select(cw => new RotateEvent(cw)),
                        _playerInput.GameStartStream.Select(_ => new StartNewGameEvent()),
                        _playerInput.GamePauseResumeStream.Select(p => new PauseResumeEvent(p))
                    });
            }
        }
    }
}
