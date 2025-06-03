﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Tetra4bica.Core;
using Tetra4bica.Graphics;
using Tetra4bica.Init;
using UniRx;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.TestTools;
using Zenject;
using static Sergei.Safonov.Utility.VectorExt;

namespace Tetra4bica.Tests
{

    public class ColorTableViewTest : ZenjectIntegrationTestFixture
    {

        private Mock<IGameEvents> _gEventsMock;

        private Mock<IObjectPool<GameObject>> _brickPoolMock;
        private Mock<IObjectPool<GameObject>> _explosionPoolMock;

        private Subject<Vector2Int> _gameStartedStream;
        private Subject<Cell> _newCellStream;
        private Subject<CellColor?[]> _mapScrollStream;
        private Subject<Cell> _eliminatedBricksStream;

        private GameObject _cellsParent;

        private List<GameObject> _spawnedBricks = new();
        private List<GameObject> _explosions = new();

        private VisualSettings _visSettings;

        public void CommonInstall()
        {
            PreInstall();

            _cellsParent = new GameObject();

            // Mocking
            // GameEvents
            _gameStartedStream = new Subject<Vector2Int>();
            _newCellStream = new Subject<Cell>();
            _mapScrollStream = new Subject<CellColor?[]>();
            _eliminatedBricksStream = new Subject<Cell>();
            _gEventsMock = new Mock<IGameEvents>();
            _gEventsMock.Setup(ev => ev.NewCellStream).Returns(_newCellStream);
            _gEventsMock.Setup(ev => ev.GameStartedStream).Returns(_gameStartedStream);
            _gEventsMock.Setup(ev => ev.TableScrollStream).Returns(_mapScrollStream);
            _gEventsMock.Setup(ev => ev.EliminatedBricksStream).Returns(_eliminatedBricksStream);
            Container.Bind<IGameEvents>().FromInstance(_gEventsMock.Object).AsSingle();

            // Settings
            _visSettings = new VisualSettings(1f, Vector3.zero, _cellsParent.transform);
            Container.BindInstance<VisualSettings>(_visSettings).AsSingle();

            // Pools
            _brickPoolMock = new Mock<IObjectPool<GameObject>>();
            _brickPoolMock.Setup(bPool => bPool.Get())
                .Returns(() =>
                {
                    var go = new GameObject();
                    go.AddComponent<SpriteRenderer>();
                    _spawnedBricks.Add(go);
                    return go;
                }
            );
            Container.Bind<IObjectPool<GameObject>>().WithId(PoolId.GAME_CELLS)
                .FromInstance(_brickPoolMock.Object).AsCached();

            _explosionPoolMock = new Mock<IObjectPool<GameObject>>();
            _explosionPoolMock.Setup(explPool => explPool.Get())
                .Returns(() =>
                {
                    var go = new GameObject();
                    go.AddComponent<ParticleSystem>();
                    _explosions.Add(go);
                    return go;
                }
            );
            Container.Bind<IObjectPool<GameObject>>().WithId(PoolId.WALL_CELL_EXPLOSION)
                .FromInstance(_explosionPoolMock.Object).AsCached();

            // Real instance to be tested
            Container.Bind<ColorTableView>().FromNewComponentOnNewGameObject().AsTransient();

            PostInstall();
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (var g in _spawnedBricks)
            {
                GameObject.Destroy(g);
            }
            _spawnedBricks.Clear();

            foreach (var g in _explosions)
            {
                GameObject.Destroy(g);
            }
            _explosions.Clear();

            GameObject.Destroy(_cellsParent);
        }


        [UnityTest]
        public IEnumerator GameStartTest()
        {

            CommonInstall();

            // start the game
            _gameStartedStream.OnNext(new Vector2Int(2, 2));

            yield return null;

            // Verification
            testColorTable(new CellColor?[,] {
                { null, null },
                { null, null }
            });
        }

        [UnityTest]
        public IEnumerator ScrollTest()
        {

            CommonInstall();

            // start the game
            _gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Scroll 1
            _mapScrollStream.OnNext(new CellColor?[] { CellColor.PaleBlue, CellColor.Magenta });
            testColorTable(new CellColor?[,] { {null, null},
                                { CellColor.PaleBlue, CellColor.Magenta } });
            // Scroll 2
            _mapScrollStream.OnNext(new CellColor?[] { CellColor.Red, null });
            testColorTable(new CellColor?[,] { {CellColor.PaleBlue, CellColor.Magenta },
                                { CellColor.Red, null } });
            // Scroll 3
            _mapScrollStream.OnNext(new CellColor?[] { CellColor.Green, null });
            testColorTable(new CellColor?[,] { {CellColor.Red, null },
                                { CellColor.Green, null } });

            yield return null;
        }

        [UnityTest]
        public IEnumerator ScrollNotStartedTest()
        {

            CommonInstall();

            // Scroll
            _mapScrollStream.OnNext(new CellColor?[] { CellColor.PaleBlue, CellColor.Magenta });

            // Verification
            testColorTable(new CellColor?[0, 0]);

            LogAssert.Expect(LogType.Error, "Trying to scroll the table before the game started!");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PatchTest()
        {

            CommonInstall();

            // start the game
            _gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Patch 1
            _newCellStream.OnNext(new Cell(v2i(1, 0), CellColor.PaleBlue));
            _newCellStream.OnNext(new Cell(v2i(0, 1), CellColor.Magenta));
            testColorTable(new CellColor?[,] { {null, CellColor.Magenta },
                                { CellColor.PaleBlue, null } });

            // Patch 2
            _newCellStream.OnNext(new Cell(v2i(0, 0), CellColor.Red));
            _newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Blue));
            testColorTable(new CellColor?[,] { { CellColor.Red, CellColor.Magenta },
                                {CellColor.PaleBlue, CellColor.Blue} });

            // Patch 3
            _newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Green));
            testColorTable(new CellColor?[,] { {CellColor.Red, CellColor.Magenta},
                                { CellColor.PaleBlue, CellColor.Green } });

            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplosionTest()
        {

            CommonInstall();

            // start the game
            _gameStartedStream.OnNext(new Vector2Int(2, 2));

            // Explosion 1
            _eliminatedBricksStream.OnNext(new Cell(v2i(0, 1), CellColor.Magenta));
            testColorTable(new CellColor?[,] { {null, null },
                                { null, null } });
            Assert.AreEqual(true, _explosions.First().GetComponent<ParticleSystem>().isPlaying);

            // Patch 1
            _newCellStream.OnNext(new Cell(v2i(0, 0), CellColor.Red));
            _newCellStream.OnNext(new Cell(v2i(1, 1), CellColor.Blue));
            // Explosion 2
            _eliminatedBricksStream.OnNext(new Cell(v2i(1, 1), CellColor.Magenta));
            testColorTable(new CellColor?[,] { {CellColor.Red, null },
                                { null, null } });
            Assert.AreEqual(true, _explosions.Last().GetComponent<ParticleSystem>().isPlaying);

            Assert.AreEqual(2, _explosions.Count());

            yield return null;
        }

        private void testColorTable(CellColor?[,] reqTable)
        {
            // Verification
            int reqWidth = reqTable.GetLength(0);
            int reqHeight = reqTable.GetLength(1);
            _brickPoolMock.Verify(bPool => bPool.Get(), Times.Exactly(reqWidth * reqHeight));
            Assert.AreEqual(reqWidth * reqHeight, _spawnedBricks.Count);

            GameObject[,] callTable = getCellTable(_spawnedBricks, reqWidth, reqHeight);
            Vector3 parentPos = _cellsParent.transform.position;
            float cellSize = _visSettings.CellSize;
            for (int x = 0; x < reqWidth; x++)
            {
                for (int y = 0; y < reqHeight; y++)
                {
                    verifyCell(
                        callTable[x, y], parentPos, v2i(x, y), cellSize,
                        reqTable[x, y].HasValue, _cellsParent.transform,
                        Cells.ToUnityColor(reqTable[x, y].GetValueOrDefault())
                    );
                }
            }
        }

        private void verifyCell(GameObject cell, Vector2 shift, Vector2Int cellPos, float cellSize, bool active,
            Transform parent, Color reqColor)
        {
            Vector3 pos = cell.transform.position;
            Vector2 reqPos = shift + v2(cellPos.x * cellSize, cellPos.y * cellSize);
            Assert.AreEqual(reqPos.x, pos.x, double.Epsilon);
            Assert.AreEqual(reqPos.y, pos.y, double.Epsilon);

            Assert.AreEqual(true, cell.activeInHierarchy);
            Assert.AreSame(parent, cell.transform.parent);
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreEqual(active, renderer.enabled);
            if (active)
            {
                Assert.AreEqual(reqColor, renderer.color);
            }
        }

        private GameObject[,] getCellTable(List<GameObject> spawnedBricks, int width, int height)
        {
            spawnedBricks.Sort(sortCellsXY);
            GameObject[,] res = new GameObject[width, height];

            List<GameObject>.Enumerator cellsEnum = spawnedBricks.GetEnumerator();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (cellsEnum.MoveNext())
                    {
                        res[x, y] = cellsEnum.Current;
                    }
                    else
                    {
                        Assert.Fail();
                    }

                }
            }

            return res;
        }

        private static int sortCellsXY(GameObject go1, GameObject go2)
        {
            if (go1 == go2)
                return 0;
            if (go1 == null)
                return -1;
            if (go2 == null)
                return 1;
            if (go1.transform.position.x == go2.transform.position.x)
            {
                return Math.Sign(go1.transform.position.y - go2.transform.position.y);
            }
            else
            {
                return Math.Sign(go1.transform.position.x - go2.transform.position.x);
            }
        }
    }
}
