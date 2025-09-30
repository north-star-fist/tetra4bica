using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Sergei.Safonov.Utility;
using Tetra4bica.Core;
using UniRx;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;
using static Tetra4bica.Core.GameLogic;
using static Tetra4bica.Input.PlayerInput;
using static Tetra4bica.Core.CellColor;

public class GameLogicTest
{

    readonly CellColor?[] _emptyWall8 = Enumerable.Repeat((CellColor?)null, 8).ToArray();

    GameLogic _gameLogic;
    TestEventProvider _eventProvider;
    Mock<ICellGenerator> _cellGeneratorMock;
    StreamItemCollector<GamePhase> _gamePhaseCollector;
    StreamItemCollector<Vector2> _projectilesCollector;
    StreamItemCollector<Cell> _eliminatedCellsCollecter;
    StreamItemCollector<PlayerTetromino> _plTetromonoCollector;
    StreamItemCollector<Unit> _gameOverCollector;
    StreamItemCollector<Cell> _newCellsCollector;

    [Test]
    public void TestProjectileFlightTrack()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 10, projectileSpeed: 1);

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new ShotEvent());

        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // Flying outside
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.AreEqual(
            new Vector2Int[] { v2i(4, 4), v2i(5, 4), v2i(6, 4), v2i(7, 4), v2i(8, 4) }.ToArray(),
            _projectilesCollector.Items.Select(v => v.toVector2Int()).ToArray()
        );
    }

    [Test]
    public void TestOneShotStickTetrominoElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 5, projectileSpeed: 1);

        var wall = new CellColor?[] {null, PaleBlue, PaleBlue, PaleBlue, null, PaleBlue, PaleBlue, PaleBlue };
        _cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(5));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(4));

        Assert.True(
            new Vector2Int[] { v2i(7, 1), v2i(7, 2), v2i(7, 3), v2i(7, 4) }.HasSameContent(
            _eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }


    [Test]
    public void TestTwoShotsTTetrominoElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, PaleBlue, null, null, null, Magenta, PaleBlue, PaleBlue },
            new CellColor?[] { null, PaleBlue, null, PaleBlue, Magenta, PaleBlue, PaleBlue, PaleBlue }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));

        // Two sequential shots to add couple of cells up to T tetromino
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));

        Assert.True(
            (new Vector2Int[] { v2i(5, 4), v2i(6, 4), v2i(7, 4), v2i(6, 5) }).HasSameContent(
            _eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void TestZTetrominoEliminationAlmostWholeRegion()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 3, projectileSpeed: 8);
        var wall = new CellColor?[] {null, null, null, null, Red, Red, Red, null };
        _cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.True(
            (new Cell[] {
                new Cell(v2i(6, 4), Red)
            })
            .HasSameContent(_newCellsCollector.Items.ToArray())
        );
        Assert.True(
            new Vector2Int[] { v2i(6, 3), v2i(6, 4), v2i(7, 4), v2i(7, 5) }.HasSameContent(
            _eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void TestZTetrominoEliminationLargeRegion()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 3, projectileSpeed: 8);
        var wall = new CellColor?[] {null, null, null, null, Red, Red, Red, Red };
        _cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1.1f));

        Assert.True(
            (new Cell[] {
                        new Cell(v2i(6, 4), Red)
            })
            .HasSameContent(_newCellsCollector.Items.ToArray())
        );
        Assert.True(
            new Vector2Int[] { v2i(6, 3), v2i(6, 4), v2i(7, 4), v2i(7, 5) }.HasSameContent(
            _eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void Test_TwoPaleBlueCellTable_TwoProjectlesShotSimultaneously_OneSticksAnotherSlides()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 3, projectileSpeed: 8);
        var wall = new CellColor?[] { null, null, null, null, null, PaleBlue, PaleBlue, null };
        _cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Up)));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f/16));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.True(_eliminatedCellsCollecter.Items.Count == 0);
        Assert.True(
            (new Cell[] {
                //new Cell(v2i(7, 6), PaleBlue),
                //new Cell(v2i(7, 5), PaleBlue),
                new Cell(v2i(7, 4), PaleBlue),
                //new Cell(v2i(7, 3), PaleBlue)
            })
            .HasSameContent(_newCellsCollector.Items.ToArray())
        );
    }

    [Test]
    public void TestPlayerRotationInWrongPlace()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(5, 3), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Green, null, null, null, null, null},
            _emptyWall8,
            _emptyWall8,
            new CellColor?[] {null, PaleBlue, PaleBlue, PaleBlue, null, PaleBlue, PaleBlue, PaleBlue }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());

        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(20));
        MovementInput moveDown = new MovementInput(
            MovementInput.HorizontalInput.None,
            MovementInput.VerticalInput.Down
        );
        _eventProvider.Publisher.OnNext(new MotionEvent(moveDown));
        _eventProvider.Publisher.OnNext(new MotionEvent(moveDown));
        _eventProvider.Publisher.OnNext(new RotateEvent(true));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        Assert.AreEqual(1, _gameOverCollector.Items.Count());
        Assert.AreEqual(3, _plTetromonoCollector.Items.Count());
        PlayerTetromino playrTetromino = _plTetromonoCollector.Items.Skip(2).First();
        Assert.True(
            new Vector2Int[] { v2i(5, 1), v2i(5, 2), v2i(5, 3), v2i(6, 2) }.HasSameContent(
            playrTetromino)
        );
    }

    [Test]
    public void TestEliminationStickTetrominoFirst()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.PaleBlue, null,
                CellColor.PaleBlue, CellColor.PaleBlue, null, null
            },
            new CellColor?[] {null, null, null, CellColor.Green,
                null, null, null, null
            }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        IEnumerable<Vector2Int> eliminatedCells = _eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(6, 2), v2i(6, 3), v2i(6, 4), v2i(6, 5) }).HasSameContent(eliminatedCells)
        );
    }

    [Test]
    public void TestFastProjectileLanding()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Green, null,
                null, null, null, null
            },
            new CellColor?[] {null, null, CellColor.PaleBlue, null,
                CellColor.PaleBlue, CellColor.PaleBlue, null, null
            }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        IEnumerable<Vector2Int> eliminatedCells = _eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.AreEqual(0, eliminatedCells.Count());
        IEnumerable<Cell> newCells = _newCellsCollector.Items;
        Assert.AreEqual(1, newCells.Count());
        Assert.AreEqual(new Cell(v2i(6, 3), CellColor.Green), newCells.First());
    }

    [Test]
    public void TestSeveralFastProjectileLanding()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 2), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Green, Green, Green, null, null, null }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(5));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.33f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.33f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.34f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3f));

        IEnumerable<Vector2Int> eliminatedCells = _eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.AreEqual(0, eliminatedCells.Count());
        IEnumerable<Cell> newCells = _newCellsCollector.Items;
        Assert.AreEqual(4, newCells.Count());
        Assert.AreEqual(new Cell(v2i(6, 3), CellColor.Green), newCells.First());
    }

    [Test]
    public void Test_TwoFastProjectilesLandingNearSameCellAboveAndLeft_BothAreLandedCloseToCell()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 2), scrollTime: 1, projectileSpeed: 2);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Green, null, null, null, null, null }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(
            new MotionEvent(new MovementInput(MovementInput.HorizontalInput.None, MovementInput.VerticalInput.Down))
        );
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.01f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        for (int i = 0; i < 90; i++)
        {
            _eventProvider.Publisher.OnNext(new FrameUpdateEvent(3f/90));
        }

        IEnumerable<Cell> newCells = _newCellsCollector.Items;
        Assert.AreEqual(2, newCells.Count());
        Assert.AreEqual(new Cell(v2i(5, 2), CellColor.Green), newCells.First());
        Assert.AreEqual(new Cell(v2i(6, 3), CellColor.Green), newCells.Last());
    }

    [Test]
    public void TestProjectileLandsWhenTableShifts()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 2, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Red, null, null, null, null, null},
            new CellColor?[] {null, null, Red, null, null, null, null, null},
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.2f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1.8f));

        IEnumerable<Vector2Int> newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(6, 3) }).HasSameContent(newCells)
        );
        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(5, 3), v2i(6, 3), v2i(6, 2), v2i(7, 2) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestProjectileDoesNotFlyAwayDuringLongFrameUpdate()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 1, projectileSpeed: 2);
        setUpCellColumns(new CellColor?[][] {
            new CellColor ?[] {null, null, Red, null, null, null, null, null },
            new CellColor ?[] {null, null, Red, null, null, null, null, null },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        var newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True((new Vector2Int[] { v2i(6, 3) }).HasSameContent(newCells));
    }

    [Test]
    public void TestScrollOfFrozenCells()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(2, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Magenta, null, null, null, null, null }
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.025f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.025f));
        // Two more cells are frozen down near that single cell
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // Every cell should be correctrly moved

        IEnumerable<Vector2Int> newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(5, 2), v2i(6, 2) })
            .HasSameContent(newCells)
        );
        Assert.AreEqual(Magenta, _gameLogic.GameState.GameTable[v2i(4, 2)]);
        Assert.AreEqual(Magenta, _gameLogic.GameState.GameTable[v2i(5, 2)]);
        Assert.AreEqual(Magenta, _gameLogic.GameState.GameTable[v2i(6, 2)]);
    }

    [Test]
    public void TestEliminationScrolledT()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Magenta, null, null, null, null, null },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Up)));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        // Two more cells are frozen down near that single cell
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Vector2Int> newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        //Assert.True((new Vector2Int[] { v2i(4, 3), v2i(5, 2) }).HasSameContent(newCells));
        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(3, 2), v2i(4, 2), v2i(5, 2), v2i(4, 3) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestEliminationScrolledL()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
             new CellColor?[] {null, null, null, null, Orange, null, null, null },
             new CellColor?[] {null, null, Orange, null, null, null, null, null },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Up)));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));

        IEnumerable<Vector2Int> newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True((new Vector2Int[] { v2i(6, 2) }).HasSameContent(newCells));
        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(5, 4), v2i(5, 3), v2i(5, 2), v2i(6, 2) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestEliminationL()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 4, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
             new CellColor?[] {null, null, Orange, null, null, null, null, null },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(4));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));

        IEnumerable<Vector2Int> newCells = _newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True((new Vector2Int[] { v2i(6, 2), v2i(5, 2) }).HasSameContent(newCells));
        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(6, 2), v2i(5, 2), v2i(5, 1), v2i(7, 2) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestEliminationScrolledBackL()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, Blue, null, null, null, null, null },
            new CellColor?[] {null, Blue, Blue, null, null, null, null, null },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(2f));

        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(5, 2), v2i(6, 2), v2i(7, 2), v2i(7, 1) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 16);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] { Red, Orange, null, Yellow, Green, PaleBlue, Blue, Magenta },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.5f));
        // It should be 2 frames. One for column scroll and another for elimination of it
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(7, 0), v2i(7, 1), v2i(7, 2), v2i(7, 3), v2i(7, 4), v2i(7, 5), v2i(7, 6), v2i(7, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallEliminationWithPlayerTetrominoBodyOnMove()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] { Red, Orange, null, Yellow, Green, PaleBlue, Blue, Magenta },
            _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(7, 0), v2i(7, 1), v2i(7, 3), v2i(7, 4), v2i(7, 5), v2i(7, 6), v2i(7, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallEliminationWithPlayerTetrominoBodyOnScroll()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] { Red, Orange, null, Yellow, Green, PaleBlue, Blue, Magenta },
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(4));
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(.4f));

        var eliminatedCells = _eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(4, 0), v2i(4, 1), v2i(4, 3), v2i(4, 4), v2i(4, 5), v2i(4, 6), v2i(4, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestFreezingParticleOnBorder()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new RotateEvent(true));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Cell> newCells = _newCellsCollector.Items.ToArray();
        Assert.True((new Cell[] { new Cell(v2i(1, 0), CellColor.Green) }).HasSameContent(newCells));
    }

    [Test]
    public void TestShootingAtBorder()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        _eventProvider.Publisher.OnNext(new RotateEvent(true));
        _eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        _eventProvider.Publisher.OnNext(new ShotEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Cell> newCells = _newCellsCollector.Items.ToArray();
        Assert.AreEqual(0, newCells.Count());
    }

    [Test]
    public void TestGameOverByNewColumnScroll()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(6, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[]{null, null, CellColor.Red, null, null, null, null, null},
            _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8, _emptyWall8
        });

        // Imitation
        _eventProvider.Publisher.OnNext(new StartNewGameEvent());
        _eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<GamePhase> collectedPhases = _gamePhaseCollector.Items.ToArray();
        Assert.AreEqual(new GamePhase[] { GamePhase.Started, GamePhase.GameOver }, collectedPhases);
    }

    // Common setup that is invoked 'manually'
    private void setUpGame(int w, int h, Vector2Int playerLocation, float scrollTime, float projectileSpeed)
    {
        GameSettings gameSettings = new GameSettings(
            w, h, scrollTime,
            playerLocation, CellColor.Yellow,
            CellColor.Green, projectileSpeed,
            true, true
        );
        _eventProvider = new TestEventProvider();
        _cellGeneratorMock = new Mock<ICellGenerator>();
        ICellGenerator testCellGenerator = _cellGeneratorMock.Object;
        _gameLogic = new GameLogic(gameSettings, _eventProvider, new TetrominoPatterns(), testCellGenerator);

        _gamePhaseCollector = new StreamItemCollector<GamePhase>();
        _gameLogic.GamePhaseStream.Subscribe(_gamePhaseCollector);

        _projectilesCollector = new();
        _gameLogic.ProjectileCoordinatesStream.Subscribe(_projectilesCollector);

        _eliminatedCellsCollecter = new();
        _gameLogic.EliminatedBricksStream.Subscribe(_eliminatedCellsCollecter);

        _plTetromonoCollector = new StreamItemCollector<PlayerTetromino>();
        _gameLogic.PlayerTetrominoStream.Subscribe(_plTetromonoCollector);
        _gameOverCollector = new StreamItemCollector<Unit>();
        _gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            .Subscribe(_gameOverCollector);
        _newCellsCollector = new StreamItemCollector<Cell>();
        _gameLogic.NewCellStream.Subscribe(_newCellsCollector);
    }

    // Mocks cell columns generation
    private void setUpCellColumns(CellColor?[][] walls)
    {
        int wallCounter = 0;
        _cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) =>
            {
                if (wallCounter < walls.Length) {
                    Array.Copy(walls[wallCounter++], buffer, buffer.Length);
                }
                else
                {
                    Array.Fill(buffer, null);
                }
            });
    }

    private class StreamItemCollector<T> : IObserver<T>
    {

        public List<T> Items = new List<T>();

        public void OnCompleted() { }
        public void OnError(Exception error) => Assert.Fail(error.Message);
        public void OnNext(T value)
        {
            Items.Add(value);
        }
    }
    internal class TestEventProvider : IGameInputEventProvider
    {

        public ISubject<IGameInputEvent> Publisher = new Subject<IGameInputEvent>();

        public IObservable<IGameInputEvent> GetInputStream() => Publisher;
    }
}

