using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tetra4bica.Core;
using Tetra4bica.Graphics;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.TestTools;
using Zenject;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Tests {

    public class ColorTableViewTest : ZenjectIntegrationTestFixture {

        Mock<IGameEvents> gEventsMock;

        Mock<IObjectPool<GameObject>> brickPoolMock;
        Mock<IObjectPool<GameObject>> explosionPoolMock;

        Subject<Vector2Int> gameStartedStream;
        Subject<Cell> newCellStream;
        Subject<CellColor[]> mapScrollStream;
        Subject<Cell> eliminatedBricksStream;

        GameObject cellsParent;

        List<GameObject> spawnedBricks = new();
        List<GameObject> explosions = new();

        VisualSettings visSettings;

        public void CommonInstall() {
            PreInstall();

            cellsParent = new GameObject();

            // Mocking
            // GameEvents
            gameStartedStream = new Subject<Vector2Int>();
            newCellStream = new Subject<Cell>();
            mapScrollStream = new Subject<CellColor[]>();
            eliminatedBricksStream = new Subject<Cell>();
            gEventsMock = new Mock<IGameEvents>();
            gEventsMock.Setup(ev => ev.NewCellStream).Returns(newCellStream);
            gEventsMock.Setup(ev => ev.GameStartedStream).Returns(gameStartedStream);
            gEventsMock.Setup(ev => ev.TableScrollStream).Returns(mapScrollStream);
            gEventsMock.Setup(ev => ev.EliminatedBricksStream).Returns(eliminatedBricksStream);
            Container.Bind<IGameEvents>().FromInstance(gEventsMock.Object).AsSingle();

            // Settings
            visSettings = new VisualSettings(1f, Vector3.zero, cellsParent.transform);
            Container.BindInstance<VisualSettings>(visSettings).AsSingle();

            // Pools
            brickPoolMock = new Mock<IObjectPool<GameObject>>();
            brickPoolMock.Setup(bPool => bPool.Get())
                .Returns(() => {
                    var go = new GameObject();
                    go.AddComponent<SpriteRenderer>();
                    spawnedBricks.Add(go);
                    return go;
                }
            );
            Container.Bind<IObjectPool<GameObject>>().WithId(PoolId.GAME_CELLS)
                .FromInstance(brickPoolMock.Object).AsCached();

            explosionPoolMock = new Mock<IObjectPool<GameObject>>();
            explosionPoolMock.Setup(explPool => explPool.Get())
                .Returns(() => {
                    var go = new GameObject();
                    go.AddComponent<ParticleSystem>();
                    explosions.Add(go);
                    return go;
                }
            );
            Container.Bind<IObjectPool<GameObject>>().WithId(PoolId.WALL_CELL_EXPLOSION)
                .FromInstance(explosionPoolMock.Object).AsCached();

            // Real instance to be tested
            Container.Bind<ColorTableView>().FromNewComponentOnNewGameObject().AsTransient();

            PostInstall();
        }

        [TearDown]
        public void CleanUp() {
            foreach (var g in spawnedBricks) {
                GameObject.Destroy(g);
            }
            spawnedBricks.Clear();

            foreach (var g in explosions) {
                GameObject.Destroy(g);
            }
            explosions.Clear();

            GameObject.Destroy(cellsParent);
        }

        [Inject]
        public ColorTableView tableView;

        [UnityTest]
        public IEnumerator GameStartTest() {

            CommonInstall();

            // start the game
            gameStartedStream.OnNext(new Vector2Int(2, 2));

            yield return null;

            // Verification
            testColorTable(new[,] { {(false, CellColor.NONE), (false, CellColor.NONE) },
                                {(false, CellColor.NONE), (false, CellColor.NONE) } });
        }

        [UnityTest]
        public IEnumerator ScrollTest() {

            CommonInstall();

            // start the game
            gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Scroll 1
            mapScrollStream.OnNext(new[] { CellColor.PaleBlue, CellColor.Magenta });
            testColorTable(new[,] { {(false, CellColor.NONE), (false, CellColor.NONE) },
                                { (true, CellColor.PaleBlue), (true, CellColor.Magenta) } });
            // Scroll 2
            mapScrollStream.OnNext(new[] { CellColor.Red, CellColor.NONE });
            testColorTable(new[,] { {(true, CellColor.PaleBlue), (true, CellColor.Magenta) },
                                { (true, CellColor.Red), (false, CellColor.NONE) } });
            // Scroll 3
            mapScrollStream.OnNext(new[] { CellColor.Green, CellColor.NONE });
            testColorTable(new[,] { {(true, CellColor.Red), (false, CellColor.NONE) },
                                { (true, CellColor.Green), (false, CellColor.NONE) } });

            yield return null;
        }

        [UnityTest]
        public IEnumerator ScrollNotStartedTest() {

            CommonInstall();

            // start the game
            //gameStartedStream.OnNext(new Vector2Int(2, 2));
            // Scroll
            mapScrollStream.OnNext(new[] { CellColor.PaleBlue, CellColor.Magenta });

            //yield return null;

            // Verification
            testColorTable(new (bool, CellColor)[0, 0]);

            LogAssert.Expect(LogType.Error, "Trying to scroll the table before the game started!");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PatchTest() {

            CommonInstall();

            // start the game
            gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Patch 1
            newCellStream.OnNext(new Cell(v2i(1, 0), CellColor.PaleBlue));
            newCellStream.OnNext(new Cell(v2i(0, 1), CellColor.Magenta));
            testColorTable(new[,] { {(false, CellColor.NONE), (true, CellColor.Magenta) },
                                { (true, CellColor.PaleBlue), (false, CellColor.NONE) } });

            // Patch 2
            newCellStream.OnNext(new Cell(v2i(0, 0), CellColor.Red));
            newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Blue));
            testColorTable(new[,] { {(true, CellColor.Red), (true, CellColor.Magenta) },
                                { (true, CellColor.PaleBlue), (true, CellColor.Blue) } });

            // Patch 3
            newCellStream.OnNext(new Cell(v2i(0, 1), CellColor.NONE));
            newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Green));
            testColorTable(new[,] { {(true, CellColor.Red), (false, CellColor.NONE) },
                                { (true, CellColor.PaleBlue), (true, CellColor.Green) } });

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplosionTest() {

            CommonInstall();

            // start the game
            gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Explosion 1
            eliminatedBricksStream.OnNext(new Cell(v2i(0, 1), CellColor.Magenta));
            testColorTable(new[,] { {(false, CellColor.NONE), (false, CellColor.NONE) },
                                { (false, CellColor.NONE), (false, CellColor.NONE) } });
            Assert.AreEqual(true, explosions.First().GetComponent<ParticleSystem>().isPlaying);

            // Patch 1
            newCellStream.OnNext(new Cell(v2i(0, 0), CellColor.Red));
            newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Blue));
            // Explosion 2
            eliminatedBricksStream.OnNext(new Cell(v2i(1, 1), CellColor.Magenta));
            testColorTable(new[,] { {(true, CellColor.Red), (false, CellColor.NONE) },
                                { (false, CellColor.NONE), (false, CellColor.NONE) } });
            Assert.AreEqual(true, explosions.Last().GetComponent<ParticleSystem>().isPlaying);

            Assert.AreEqual(2, explosions.Count());

            yield return null;
        }

        private void testColorTable((bool active, CellColor color)[,] reqTable) {
            // Verification
            int reqWidth = reqTable.GetLength(0);
            int reqHeight = reqTable.GetLength(1);
            brickPoolMock.Verify(bPool => bPool.Get(), Times.Exactly(reqWidth * reqHeight));
            Assert.AreEqual(reqWidth * reqHeight, spawnedBricks.Count);

            GameObject[,] callTable = getCellTable(spawnedBricks, reqWidth, reqHeight);
            Vector3 parentPos = cellsParent.transform.position;
            float cellSize = visSettings.cellSize;
            for (int x = 0; x < reqWidth; x++) {
                for (int y = 0; y < reqHeight; y++) {
                    verifyCell(
                        callTable[x, y], parentPos, v2i(x, y), cellSize,
                        reqTable[x, y].active, cellsParent.transform,
                        Cells.ToUnityColor(reqTable[x, y].color)
                    );
                }
            }
        }

        private void verifyCell(GameObject cell, Vector2 shift, Vector2Int cellPos, float cellSize, bool active,
            Transform parent, Color reqColor) {
            Vector3 pos = cell.transform.position;
            Vector2 reqPos = shift + v2(cellPos.x * cellSize, cellPos.y * cellSize);
            Assert.AreEqual(reqPos.x, pos.x, double.Epsilon);
            Assert.AreEqual(reqPos.y, pos.y, double.Epsilon);

            Assert.AreEqual(true, cell.activeInHierarchy);
            //Assert.AreEqual(active, cell.activeInHierarchy);
            Assert.AreSame(parent, cell.transform.parent);
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            if (active) {
                Assert.AreEqual(active, renderer.enabled);
                Assert.AreEqual(reqColor, renderer.color);
            }
        }

        private GameObject[,] getCellTable(List<GameObject> spawnedBricks, int width, int height) {
            spawnedBricks.Sort(sortCellsXY);
            GameObject[,] res = new GameObject[width, height];

            List<GameObject>.Enumerator cellsEnum = spawnedBricks.GetEnumerator();
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (cellsEnum.MoveNext()) {
                        res[x, y] = cellsEnum.Current;
                    } else {
                        Assert.Fail();
                    }

                }
            }

            return res;
        }

        private static int sortCellsXY(GameObject go1, GameObject go2) {
            if (go1 == go2)
                return 0;
            if (go1 == null)
                return -1;
            if (go2 == null)
                return 1;
            if (go1.transform.position.x == go2.transform.position.x) {
                return Math.Sign(go1.transform.position.y - go2.transform.position.y);
            } else {
                return Math.Sign(go1.transform.position.x - go2.transform.position.x);
            }
        }
    }
}