using Sergei.Safonov.Utility;
using System;
using System.Collections.Generic;
using Tetra4bica.Core;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Graphics {

    public class ColorTableView : MonoBehaviour {

        [Inject]
        IGameEvents gameEvents;

        [Inject]
        VisualSettings visualSettings;

        [Inject(Id = PoolId.GAME_CELLS)]
        IObjectPool<GameObject> bricksPool;

        [Inject(Id = PoolId.WALL_CELL_EXPLOSION)]
        IObjectPool<GameObject> wallBrickExplosionParticlesPool;


        // Brick instances on the map.
        Dictionary<Vector2Int, SpriteRenderer> brickMap = new Dictionary<Vector2Int, SpriteRenderer>();

        Vector2Int mapSize;

        bool started;

        private void Awake() {
            Setup(
                gameEvents.GameStartedStream,
                gameEvents.NewCellStream,
                gameEvents.TableScrollStream,
                gameEvents.EliminatedBricksStream
            );
        }

        void Setup(
            IObservable<Vector2Int> gameStartedStream,
            IObservable<Cell> newCellStream,
            IObservable<IEnumerable<CellColor>> mapScrollStream,
            IObservable<Cell> eliminatedBricksStream
        ) {
            mapScrollStream.Subscribe(scrollMap);
            newCellStream.Subscribe((c) => updateBrickVisuals(c));
            eliminatedBricksStream.Subscribe(
                (cell) => launchDestroyedBrickAnimation(cell.Position, cell.Color)
            );
            gameStartedStream.Subscribe(mapSize => {
                if (this.mapSize == mapSize) {
                    paintAll(CellColor.NONE);
                    return;
                }
                this.mapSize = mapSize;
                instantiateBricks(
                    mapSize.x, mapSize.y,
                    visualSettings.BottomLeftPoint
                );
                paintAll(CellColor.NONE);
                started = true;
            });
        }

        private void paintAll(CellColor color) {
            for (int x = 0; x < mapSize.x; x++) {
                for (int y = 0; y < mapSize.y; y++) {
                    updateBrickVisuals(new Cell(v2i(x, y), color));
                }
            }
        }

        private void instantiateBricks(int width, int height, Vector2 bottomLeftTunnelPoint) {
            foreach (var brick in brickMap.Values) {
                bricksPool.Release(brick.gameObject);
            }
            brickMap.Clear();
            for (int x = 0; x < width; x++) {
                Vector2 wallSpawnPoint = new Vector2(
                    bottomLeftTunnelPoint.x + x * visualSettings.cellSize,
                    bottomLeftTunnelPoint.y
                );
                for (int y = 0; y < height; y++) {
                    GameObject brickInstance = instantiateBrick(wallSpawnPoint, y);
                    SpriteRenderer spriteRenderer = brickInstance.GetComponent<SpriteRenderer>()
                        ?? throw new Exception($"Cell prefab does not contain {typeof(SpriteRenderer)} component!");
                    brickMap[v2i(x, y)] = spriteRenderer;
                }
            }

            GameObject instantiateBrick(Vector2 wallSpawnPoint, int y) {
                Vector3 brickPosition = new Vector2(
                    wallSpawnPoint.x,
                    wallSpawnPoint.y + visualSettings.cellSize * y
                );
                var brickInstance = bricksPool.Get();
                brickInstance.transform.position = brickPosition;
                brickInstance.transform.parent = visualSettings.bricksParent;
                //brickInstance.SetActive(true);
                return brickInstance;
            }
        }

        private void scrollMap(IEnumerable<CellColor> newWall) {
            if (!started) {
                Debug.LogError("Trying to scroll the table before the game started!");
                return;
            }
            for (int x = 0; x < mapSize.x - 1; x++) {
                for (int y = 0; y < mapSize.y; y++) {
                    var renderer = brickMap[v2i(x, y)];
                    var rendererToTheRight = brickMap[v2i(x + 1, y)];
                    if (rendererToTheRight.gameObject.activeInHierarchy
                        && rendererToTheRight.enabled) {
                        paintCell(renderer, rendererToTheRight.color);
                    } else {
                        renderer.enabled = false;
                        //renderer.gameObject.SetActive(false);
                    }
                }
            }
            {
                int y = 0;
                foreach (var cell in newWall) {
                    if (y >= mapSize.y) {
                        Debug.LogError("Received more cells in wall than the map height!");
                        break;
                    }
                    var mostRightCell = brickMap[v2i(mapSize.x - 1, y)];
                    updateBrickVisuals(new Cell(v2i(mapSize.x - 1, y), cell));
                    y++;
                }
            }
        }

        private void updateBrickVisuals(Cell cell) {
            SpriteRenderer spriteRenderer = brickMap[cell.Position];
            if (cell.Color != CellColor.NONE) {
                paintCell(spriteRenderer, Cells.ToUnityColor(cell.Color));
            } else {
                spriteRenderer.enabled = false;
                //spriteRenderer.gameObject.SetActive(false);
            }
        }

        private static void paintCell(SpriteRenderer spriteRenderer, Color color) {
            //spriteRenderer.gameObject.SetActive(true);
            spriteRenderer.enabled = true;
            spriteRenderer.color = color;
        }

        private void launchDestroyedBrickAnimation(Vector2Int xy, CellColor eliminatedCellColor) {

            updateBrickVisuals(new Cell(xy, CellColor.NONE));
            renderWallBrickExplosion(xy);

            void renderWallBrickExplosion(Vector2Int xy) {
                GameObject cellExplParticleSystemObj = wallBrickExplosionParticlesPool.Get();
                // TODO scale
                cellExplParticleSystemObj.transform.position = xy.toVector3();
                cellExplParticleSystemObj.SetActive(true);
                ParticleSystem ps = cellExplParticleSystemObj.GetComponent<ParticleSystem>();
                if (ps != null) {
                    ps.Play();
                }
            }
        }
    }
}
