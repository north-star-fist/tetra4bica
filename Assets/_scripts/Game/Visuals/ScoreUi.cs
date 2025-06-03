using System;
using DG.Tweening;
using DG.Tweening.Core;
using Sergei.Safonov.Audio;
using Sergei.Safonov.Utility;
using Tetra4bica.Core;
using Tetra4bica.Init;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Tetra4bica.Graphics
{

    public class ScoreUi : MonoBehaviour
    {

        private static readonly Vector3 ParticleStartScale = Vector3.one;
        private static readonly Vector3 ParticleEndScale = Vector3.one * 0.3f;

        [Inject]
        private IGameEvents _gameEvents;

        [Inject(Id = AudioSourceId.SoundEffects)]
        private AudioSource _uiSoundsAudioSource;

        [Inject]
        private VisualSettings _visualSettings;

        [Inject(Id = PoolId.SCORE_CELLS)]
        private IObjectPool<GameObject> _scoreParticlesPool;

        [
            SerializeField,
            Tooltip("Root Rect Transform of the whole scores component (label + count)"),
            FormerlySerializedAs("scoresPanel")
        ]
        private RectTransform _scoresPanel;
        [
            SerializeField,
            Tooltip("TextMeshPro component with score counter"),
            FormerlySerializedAs("scoreCountTextTMP")
        ]
        private TMP_Text _scoreCountTextTMP;
        [
            SerializeField,
            Tooltip("Mask above the score text. Put it here todisable on game over"),
            FormerlySerializedAs("scoresBackground")
        ]
        private Image _scoresBackground;
        [SerializeField, FormerlySerializedAs("scoreGainSfx")]
        private AudioResource _scoreGainSfx;

        [SerializeField, Tooltip("Size of Scores on Game Over event"), FormerlySerializedAs("scoreRectTransformFinalScale")]
        private Vector2 _scoreRectTransformFinalScale = Vector2.one * 4;
        [SerializeField, Tooltip("Score panel ordinary position"), FormerlySerializedAs("scoreTextPosition")]
        private RectTransform _scoreTextPosition;
        [SerializeField, Tooltip("Score panel game over position"), FormerlySerializedAs("gameOverScoreTextPosition")]
        private RectTransform _gameOverScoreTextPosition;

        private Vector3 _scoreParticlesLandingWorldPosition;

        private uint _uiScores;
        private uint _trueScores;
        private TweenCellParticleWrapper[,] _cellWrappers;

        private TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> _finalPositionTween;
        private Sequence _scoresFinalAnimation;

        private bool _isWebGlPlayer;


        private void Start()
        {
            _isWebGlPlayer = Application.platform == RuntimePlatform.WebGLPlayer;
            if (_scoreCountTextTMP == null)
            {
                throw new ArgumentException($"{nameof(_scoreCountTextTMP)} is undefined");
            }
            Setup(
                _gameEvents.GameStartedStream,
                _gameEvents.EliminatedBricksStream,
                _gameEvents.ScoreStream,
                _gameEvents.GamePhaseStream.Where(g => g == GamePhase.GameOver).AsUnitObservable()
            );
        }


        void Setup(
            IObservable<Vector2Int> gameStartedStream,
            IObservable<Cell> eliminatedBricksObservable,
            IObservable<uint> scoresObservable,
            IObservable<Unit> gameOverObservable
        )
        {
            gameStartedStream.DelayFrame(2).Subscribe(size =>
            {
                createDoTweenCache(size);
                resetScoreTextTransform();
                setUiScores(0);
            });
            gameStartedStream.First().DelayFrame(1).Subscribe(
                _ =>
                {
                    updateScoresDestinationPosition();
                    initScoresFinalAnimation();
                }
            );
            eliminatedBricksObservable.Subscribe(cell => launchDestroyedBrickAnimation(cell.Position, cell.Color));
            scoresObservable.Subscribe(scores => this._trueScores = scores);
            gameOverObservable.Subscribe((_) => enlargeScores());

            void resetScoreTextTransform()
            {
                //scoresPanel.localScale = Vector3.one;
                _scoresBackground.enabled = true;
                _finalPositionTween.Rewind();
                _scoresFinalAnimation.Rewind();
                _scoresPanel.position = _scoreTextPosition.TransformPoint(_scoreTextPosition.rect.center);
            }
        }


        private void createDoTweenCache(Vector2Int size)
        {
            _cellWrappers = new TweenCellParticleWrapper[size.x, size.y];
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2 startPos = new Vector2(
                        _visualSettings.BottomLeftPoint.x + x * _visualSettings.CellSize,
                        _visualSettings.BottomLeftPoint.y + y * _visualSettings.CellSize
                    );
                    _cellWrappers[x, y] = new TweenCellParticleWrapper(
                        _scoreParticlesPool,
                        getScoreParticlesLandingWorldPosition,
                        startPos,
                        _visualSettings,
                        forEachCellEliminated
                    );

                }
            }
        }

        private void forEachCellEliminated()
        {
            if (!_isWebGlPlayer)
            {
                SoundUtils.PlaySound(_uiSoundsAudioSource, _scoreGainSfx);
            }
            if (_uiScores < _trueScores)
            {
                setUiScores(_uiScores + 1);
            }
            else
            {
                Debug.LogWarning($"There are too many cell particles! More than scores");
            }
        }

        private Vector2 getScoreParticlesLandingWorldPosition() => _scoreParticlesLandingWorldPosition;

        private void launchDestroyedBrickAnimation(Vector2Int xy, CellColor cell)
        {
            var cellWrapper = _cellWrappers[xy.x, xy.y];
            SpriteRenderer renderer = cellWrapper.GameCell.SpriteRenderer;
            renderer.color = Cells.ToUnityColor(cell);
            renderer.enabled = true;
            cellWrapper.GetTweenSequence().Restart();
        }

        private void setUiScores(uint uiScores)
        {
            if (_uiScores == uiScores)
            {
                return;
            }
            _uiScores = uiScores;
            _scoreCountTextTMP.text = uiScores.ToString("D4");
        }

        private void updateScoresDestinationPosition()
            => _scoreParticlesLandingWorldPosition
                = Camera.main.ScreenToWorldPoint(_scoreCountTextTMP.transform.position);

        private class TweenCellParticleWrapper
        {
            public GameCell GameCell => getPooledCell();

            private readonly IObjectPool<GameObject> _pool;
            private GameCell _cell;
            private readonly Func<Vector2> _scorePosition;
            private Vector2 _startPosition;
            private readonly float _flightTime;
            private readonly Sequence _scoreTweenSeq;
            private readonly Action _onComplete;



            public TweenCellParticleWrapper(
                IObjectPool<GameObject> cellPool,
                Func<Vector2> scorePosition,
                Vector2 startPos,
                VisualSettings visualSettings,
                Action onTweenComplete
            )
            {
                _pool = cellPool;
                _scorePosition = scorePosition;
                _startPosition = startPos;

                _flightTime = UnityEngine.Random.Range(
                    visualSettings.ScoreParticlesFlightTimeMin,
                    visualSettings.ScoreParticlesFlightTimeMax
                );

                _scoreTweenSeq = DOTween.Sequence()
                    .Append(GetPositionTween())
                    .Insert(0, GetScaleTween());
                _scoreTweenSeq.onComplete = () =>
                {
                    GameCell.SpriteRenderer.enabled = false;
                    _cell = null;
                    _onComplete?.Invoke();
                };
                _onComplete = onTweenComplete;
                _cell = null;    // unbinding of the temporary cell
            }

            public Sequence GetTweenSequence() => _scoreTweenSeq;

            public Tween GetPositionTween()
            {
                return DOTween.To(GetPosition, SetPosition, _startPosition, _flightTime).From();
            }

            public Tween GetScaleTween()
            {
                return DOTween.To(GetScale, SetScale, ParticleEndScale, _flightTime);
            }

            public Vector2 GetPosition()
            {
                return getPooledCell().transform.position;
            }

            public void SetPosition(Vector2 newPos)
            {
                getPooledCell().transform.position = newPos;
            }

            public Vector3 GetScale()
            {
                return getPooledCell().transform.localScale;
            }

            public void SetScale(Vector3 newScale)
            {
                getPooledCell().transform.localScale = newScale;
            }

            GameCell getPooledCell()
            {
                if (_cell == null)
                {
                    _cell = _pool.Get().GetComponent<GameCell>();
                    if (_cell == null)
                    {
                        throw new MissingComponentException($"No {nameof(GameCell)} component found");
                    }
                    _cell.SpriteRenderer.enabled = false;
                    _cell.transform.SetPositionAndRotation(_scorePosition(), Quaternion.Euler(0, 0, UnityEngine.Random.value));
                    _cell.transform.localScale = ParticleStartScale;
                }
                return _cell;
            }
        }

        private void enlargeScores()
        {
            // Canvas layout can be changed with time, especially in WebGL player where game viewport is 
            // controlled by browser. So score text positions should be synchronised appropriatelly.
            _finalPositionTween.ChangeStartValue(_scoreTextPosition.TransformPoint(_scoreTextPosition.rect.center));
            _finalPositionTween.ChangeEndValue(_gameOverScoreTextPosition.TransformPoint(_gameOverScoreTextPosition.rect.center));
            _finalPositionTween.Play();
            _scoresFinalAnimation.Play();
        }

        private void initScoresFinalAnimation()
        {
            _finalPositionTween =
                DOTween.To(
                    () => _scoresPanel.position,
                    p => _scoresPanel.position = p,
                    _gameOverScoreTextPosition.TransformPoint(_gameOverScoreTextPosition.rect.center),
                    1f
                );
            _scoresFinalAnimation = DOTween.Sequence()
                .Join(DOTween.To(() => _scoresPanel.localScale, s => _scoresPanel.localScale = s,
                    _scoreRectTransformFinalScale.toVector3(), 1f));
            _scoresFinalAnimation.onPlay = () => { _scoresBackground.enabled = false; };
            //.Join(finalPositionTween);

        }
    }
}
