using DG.Tweening;
using DG.Tweening.Core;
using Sergei.Safonov.Audio;
using Sergei.Safonov.Utility;
using System;
using Tetra4bica.Core;
using Tetra4bica.Init;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace Tetra4bica.Graphics {

    public class ScoreUi : MonoBehaviour {

        [Inject]
        IGameEvents gameEvents;

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource uiSoundsAudioSource;

        [Inject]
        VisualSettings visualSettings;

        [Inject(Id = PoolId.SCORE_CELLS)]
        IObjectPool<GameObject> scoreParticlesPool;

        [Tooltip("Root Rect Transform of the whole scores component (label + count)")]
        public RectTransform scoresPanel;
        [Tooltip("TextMeshPro component with score counter")]
        public TMP_Text scoreCountTextTMP;
        public AudioResource scoreGainSfx;

        [Tooltip("Size of Scores on Game Over event")]
        public Vector2 scoreRectTransformFinalScale = Vector2.one * 4;
        [Tooltip("Score panel ordinary position")]
        public RectTransform scoreTextPosition;
        [Tooltip("Score panel game over position")]
        public RectTransform gameOverScoreTextPosition;

        Vector3 scoreParticlesLandingWorldPosition;

        uint uiScores;
        uint trueScores;
        private TweenCellParticleWrapper[,] cellWrappers;

        TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> finalPositionTween;
        Sequence scoresFinalAnimation;

        bool isWebGlPlayer;

        private void Start() {
            isWebGlPlayer = Application.platform == RuntimePlatform.WebGLPlayer;
            if (scoreCountTextTMP == null) {
                throw new ArgumentException($"{nameof(scoreCountTextTMP)} is undefined");
            }
            Setup(
                gameEvents.GameStartedStream,
                gameEvents.EliminatedBricksStream,
                gameEvents.ScoreStream,
                gameEvents.GamePhaseStream.Where(g => g == GamePhase.GameOver).AsUnitObservable()
            );
        }


        void Setup(
            IObservable<Vector2Int> gameStartedStream,
            IObservable<Cell> eliminatedBricksObservable,
            IObservable<uint> scoresObservable,
            IObservable<Unit> gameOverObservable
        ) {
            gameStartedStream.DelayFrame(2).Subscribe(size => {
                createDoTweenCache(size);
                resetScoreTextTransform();
                setUiScores(0);
            });
            gameStartedStream.First().DelayFrame(1).Subscribe(
                _ => {
                    updateScoresDestinationPosition();
                    initScoresFinalAnimation();
                }
            );
            eliminatedBricksObservable.Subscribe(cell => launchDestroyedBrickAnimation(cell.Position, cell.Color));
            scoresObservable.Subscribe(scores => this.trueScores = scores);
            gameOverObservable.Subscribe((_) => enlargeScores());

            void resetScoreTextTransform() {
                //scoresPanel.localScale = Vector3.one;
                finalPositionTween.Rewind();
                scoresFinalAnimation.Rewind();
                scoresPanel.position = scoreTextPosition.TransformPoint(scoreTextPosition.rect.center);
            }
        }


        private void createDoTweenCache(Vector2Int size) {
            cellWrappers = new TweenCellParticleWrapper[size.x, size.y];
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Vector2 startPos = new Vector2(
                        visualSettings.BottomLeftPoint.x + x * visualSettings.cellSize,
                        visualSettings.BottomLeftPoint.y + y * visualSettings.cellSize
                    );
                    cellWrappers[x, y] = new TweenCellParticleWrapper(
                        scoreParticlesPool,
                        getScoreParticlesLandingWorldPosition,
                        startPos,
                        visualSettings,
                        forEachCellEliminated
                    );

                }
            }
        }

        private void forEachCellEliminated() {
            if (!isWebGlPlayer) {
                SoundUtils.PlaySound(uiSoundsAudioSource, scoreGainSfx);
            }
            if (uiScores < trueScores) {
                setUiScores(uiScores + 1);
            } else {
                Debug.LogWarning($"There are too many cell particles! More than scores");
            }
        }

        private Vector2 getScoreParticlesLandingWorldPosition() => scoreParticlesLandingWorldPosition;

        private void launchDestroyedBrickAnimation(Vector2Int xy, CellColor cell) {
            var cellWrapper = cellWrappers[xy.x, xy.y];
            SpriteRenderer renderer = cellWrapper.getRenderer();
            renderer.color = Cells.ToUnityColor(cell);
            renderer.enabled = true;
            cellWrapper.GetTweenSequence().Restart();
        }

        private void setUiScores(uint uiScores) {
            if (this.uiScores == uiScores) {
                return;
            }
            this.uiScores = uiScores;
            scoreCountTextTMP.text = uiScores.ToString("D4");
        }

        private void updateScoresDestinationPosition()
            => scoreParticlesLandingWorldPosition
                = Camera.main.ScreenToWorldPoint(scoreCountTextTMP.transform.position);

        private class TweenCellParticleWrapper {

            IObjectPool<GameObject> pool;
            GameObject cell;
            Func<Vector2> scorePosition;
            Vector2 startPosition;
            private float flightTime;
            private Sequence scoreTweenSeq;
            Action onComplete;

            public TweenCellParticleWrapper(
                IObjectPool<GameObject> cellPool,
                Func<Vector2> scorePosition,
                Vector2 startPos,
                VisualSettings visualSettings,
                Action onTweenComplete
            ) {
                this.pool = cellPool;
                this.scorePosition = scorePosition;
                startPosition = startPos;

                flightTime = UnityEngine.Random.Range(
                    visualSettings.scoreParticlesFlightTimeMin,
                    visualSettings.scoreParticlesFlightTimeMax
                );

                scoreTweenSeq = DOTween.Sequence()
                    .Append(getPositionTween())
                    .Insert(0, getScaleTween());
                scoreTweenSeq.onComplete = () => {
                    getRenderer().enabled = false;
                    cell = null;
                    this.onComplete?.Invoke();
                };
                this.onComplete = onTweenComplete;
                cell = null;    // unbinding of the temporary cell
            }

            public Sequence GetTweenSequence() { return scoreTweenSeq; }

            public Tween getPositionTween() {
                return DOTween.To(getPosition, setPosition, startPosition, flightTime).From();
            }

            public Tween getScaleTween() {
                return DOTween.To(getScale, setScale, (Vector2.one * 0.3f).toVector3(), flightTime);
            }

            public Vector2 getPosition() {
                return getPooledCell().transform.position;
            }

            public void setPosition(Vector2 newPos) {
                getPooledCell().transform.position = newPos;
            }

            public Vector3 getScale() {
                return getPooledCell().transform.localScale;
            }

            public void setScale(Vector3 newScale) {
                getPooledCell().transform.localScale = newScale;
            }

            GameObject getPooledCell() {
                if (cell == null) {
                    cell = pool.Get();
                    getRenderer().enabled = false;
                    cell.transform.SetPositionAndRotation(scorePosition(), Quaternion.Euler(0, 0, UnityEngine.Random.value));
                    cell.transform.localScale = Vector3.one * 7;
                }
                return cell;
            }

            public SpriteRenderer getRenderer() {
                return getPooledCell().GetComponent<SpriteRenderer>();
            }
        }

        private void enlargeScores() {
            // Canvas layout can be changed with time, especially in WebGL player where game viewport is 
            // controlled by browser. So score text positions should be synchronised appropriatelly.
            finalPositionTween.ChangeStartValue(scoreTextPosition.TransformPoint(scoreTextPosition.rect.center));
            finalPositionTween.ChangeEndValue(gameOverScoreTextPosition.TransformPoint(gameOverScoreTextPosition.rect.center));
            finalPositionTween.Play();
            scoresFinalAnimation.Play();
        }

        private void initScoresFinalAnimation() {
            finalPositionTween =
                DOTween.To(
                    () => scoresPanel.position,
                    p => scoresPanel.position = p,
                    gameOverScoreTextPosition.TransformPoint(gameOverScoreTextPosition.rect.center),
                    1f
                );
            scoresFinalAnimation = DOTween.Sequence()
                .Join(DOTween.To(() => scoresPanel.localScale, s => scoresPanel.localScale = s,
                    scoreRectTransformFinalScale.toVector3(), 1f));
            //.Join(finalPositionTween);

        }
    }
}