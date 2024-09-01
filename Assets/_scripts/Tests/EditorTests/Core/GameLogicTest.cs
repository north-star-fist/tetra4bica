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

public class GameLogicTest
{

    readonly CellColor?[] emptyWall8 = Enumerable.Repeat((CellColor?)null, 8).ToArray();

    GameLogic gameLogic;
    TestEventProvider eventProvider;
    Mock<ICellGenerator> cellGeneratorMock;
    StreamItemCollector<GamePhase> gamePhaseCollector;
    StreamItemCollector<Vector2> projectilesCollector;
    StreamItemCollector<Cell> eliminatedCellsCollecter;
    StreamItemCollector<PlayerTetromino> plTetromonoCollector;
    StreamItemCollector<Unit> gameOverCollector;
    StreamItemCollector<Cell> newCellsCollector;

    [Test]
    public void TestProjectileFlightTrack()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 10, projectileSpeed: 1);

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new ShotEvent());

        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // Flying outside
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.AreEqual(
            new Vector2Int[] { v2i(4, 4), v2i(5, 4), v2i(6, 4), v2i(7, 4), v2i(8, 4) }.ToArray(),
            projectilesCollector.Items.Select(v => v.toVector2Int()).ToArray()
        );
    }

    [Test]
    public void TestOneShotStickTetrominoElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 5, projectileSpeed: 1);

        var wall = new CellColor?[] {null, CellColor.PaleBlue, CellColor.PaleBlue, CellColor.PaleBlue,
            null, CellColor.PaleBlue, CellColor.PaleBlue, CellColor.PaleBlue
        };
        cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(5));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(4));

        Assert.True(
            new Vector2Int[] { v2i(7, 1), v2i(7, 2), v2i(7, 3), v2i(7, 4) }.HasSameContent(
            eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }


    [Test]
    public void TestTwoShotsTTetrominoElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, CellColor.PaleBlue, null, null,
                null, CellColor.Magenta, CellColor.PaleBlue, CellColor.PaleBlue
            },
            new CellColor?[] { null, CellColor.PaleBlue, null, CellColor.PaleBlue,
                CellColor.Magenta, CellColor.PaleBlue, CellColor.PaleBlue, CellColor.PaleBlue
            }
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));

        // Two sequential shots to add couple of cells up to T tetromino
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));

        Assert.True(
            (new Vector2Int[] { v2i(5, 4), v2i(6, 4), v2i(7, 4), v2i(6, 5) }).HasSameContent(
            eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void TestZTetrominoEliminationAlmostWholeRegion()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 3, projectileSpeed: 8);
        var wall = new CellColor?[] {null, null, null, null,
            CellColor.Red, CellColor.Red, CellColor.Red, null
        };
        cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.True(
            new Vector2Int[] { v2i(6, 3), v2i(6, 4), v2i(7, 4), v2i(7, 5) }.HasSameContent(
            eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void TestZTetrominoEliminationLargeRegion()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(1, 3), scrollTime: 3, projectileSpeed: 8);
        var wall = new CellColor?[] {null, null, null, null,
            CellColor.Red, CellColor.Red, CellColor.Red, CellColor.Red };
        cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(wall, buffer, buffer.Length));

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(3));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        Assert.True(
            new Vector2Int[] { v2i(6, 3), v2i(6, 4), v2i(7, 4), v2i(7, 5) }.HasSameContent(
            eliminatedCellsCollecter.Items.Select(vc => vc.Position))
        );
    }

    [Test]
    public void TestPlayerRotationInWrongPlace()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(5, 3), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Green, null, null, null, null, null},
            emptyWall8,
            emptyWall8,
            new CellColor?[] {null, CellColor.PaleBlue, CellColor.PaleBlue, CellColor.PaleBlue,
                null, CellColor.PaleBlue, CellColor.PaleBlue, CellColor.PaleBlue
            }
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());

        eventProvider.Publisher.OnNext(new FrameUpdateEvent(20));
        MovementInput moveDown = new MovementInput(
            MovementInput.HorizontalInput.None,
            MovementInput.VerticalInput.Down
        );
        eventProvider.Publisher.OnNext(new MotionEvent(moveDown));
        eventProvider.Publisher.OnNext(new MotionEvent(moveDown));
        eventProvider.Publisher.OnNext(new RotateEvent(true));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        Assert.AreEqual(1, gameOverCollector.Items.Count());
        Assert.AreEqual(3, plTetromonoCollector.Items.Count());
        PlayerTetromino playrTetromino = plTetromonoCollector.Items.Skip(2).First();
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
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        IEnumerable<Vector2Int> eliminatedCells = eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(6, 2), v2i(6, 3), v2i(6, 4), v2i(6, 5) })
            .HasSameContent(eliminatedCells)
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
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(10));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        IEnumerable<Vector2Int> eliminatedCells = eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.AreEqual(0, eliminatedCells.Count());
        IEnumerable<Cell> newCells = newCellsCollector.Items;
        Assert.AreEqual(1, newCells.Count());
        Assert.AreEqual(new Cell(v2i(6, 3), CellColor.Green), newCells.First());
    }

    [Test]
    public void TestSeveralFastProjectileLanding()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 2), scrollTime: 5, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Green, CellColor.Green,
                CellColor.Green, null, null, null
            }
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(5));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.33f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.33f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.34f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(3f));

        IEnumerable<Vector2Int> eliminatedCells = eliminatedCellsCollecter.Items.Select(vc => vc.Position).ToArray();
        Assert.AreEqual(0, eliminatedCells.Count());
        IEnumerable<Cell> newCells = newCellsCollector.Items;
        Assert.AreEqual(4, newCells.Count());
        Assert.AreEqual(new Cell(v2i(6, 3), CellColor.Green), newCells.First());
    }

    [Test]
    public void TestProjectileLandsWhenTableShifts()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 2, projectileSpeed: 1);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Red, null, null, null, null, null},
            new CellColor?[] {null, null, CellColor.Red, null, null, null, null, null},
            emptyWall8, emptyWall8 });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.2f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1.8f));

        IEnumerable<Vector2Int> newCells = newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(6, 3), v2i(7, 3) })
            .HasSameContent(newCells)
        );
    }

    [Test]
    public void TestProjectileDoesNotFlyAwayDuringLongFrameUpdate()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 2), scrollTime: 1, projectileSpeed: 2);
        setUpCellColumns(new CellColor?[][] {
            new CellColor ?[] {null, null, CellColor.Red, null, null, null, null, null },
            new CellColor ?[] {null, null, CellColor.Red, null, null, null, null, null },
            emptyWall8,
            emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));

        IEnumerable<Vector2Int> newCells = newCellsCollector.Items.Select(vc => vc.Position);
        Assert.True((new Vector2Int[] { v2i(7, 3) }).HasSameContent(newCells));
    }

    [Test]
    public void TestScrollOfFrozenCells()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(2, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Magenta, null, null, null, null, null },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.025f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.025f));
        // Two more cells are frozen down near that single cell
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // Every cell should be correctrly moved

        IEnumerable<Vector2Int> newCells = newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True(
            (new Vector2Int[] { v2i(5, 2), v2i(6, 2) })
            .HasSameContent(newCells)
        );
        Assert.AreEqual(CellColor.Magenta, gameLogic.GameState.GameTable[v2i(4, 2)]);
        Assert.AreEqual(CellColor.Magenta, gameLogic.GameState.GameTable[v2i(5, 2)]);
        Assert.AreEqual(CellColor.Magenta, gameLogic.GameState.GameTable[v2i(6, 2)]);
    }

    [Test]
    public void TestEliminationScrolledT()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Magenta, null, null, null, null, null },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Up)));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        // Two more cells are frozen down near that single cell
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Vector2Int> newCells = newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True((new Vector2Int[] { v2i(5, 3), v2i(6, 2) }).HasSameContent(newCells));
        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(3, 2), v2i(4, 2), v2i(5, 2), v2i(4, 3) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestEliminationScrolledL()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
             new CellColor?[] {null, null, null, null, CellColor.Orange, null, null, null },
             new CellColor?[] {null, null, CellColor.Orange, null, null, null, null, null },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        // One cell on the table
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Up)));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1f));

        IEnumerable<Vector2Int> newCells = newCellsCollector.Items.Select(vc => vc.Position).ToArray();
        Assert.True((new Vector2Int[] { v2i(6, 2) }).HasSameContent(newCells));
        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(5, 4), v2i(5, 3), v2i(5, 2), v2i(6, 2) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestEliminationScrolledBackL()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {null, null, CellColor.Blue, null, null, null, null, null },
            new CellColor?[] {null, CellColor.Blue, CellColor.Blue, null, null, null, null, null },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2));
        // One cell on the table
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(2f));

        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] { v2i(5, 2), v2i(6, 2), v2i(7, 2), v2i(7, 1) }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallElimination()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 16);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {
                CellColor.Red, CellColor.Orange, null, CellColor.Yellow,
                CellColor.Green, CellColor.PaleBlue, CellColor.Blue, CellColor.Magenta
            },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.5f));
        // It should be 2 frames. One for column scroll and another for elimination of it
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(7, 0), v2i(7, 1), v2i(7, 2), v2i(7, 3), v2i(7, 4), v2i(7, 5), v2i(7, 6), v2i(7, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallEliminationWithPlayerTetrominoBodyOnMove()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {CellColor.Red, CellColor.Orange, null, CellColor.Yellow,
                CellColor.Green, CellColor.PaleBlue, CellColor.Blue, CellColor.Magenta },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.1f));

        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(7, 0), v2i(7, 1), v2i(7, 3), v2i(7, 4), v2i(7, 5), v2i(7, 6), v2i(7, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestWallEliminationWithPlayerTetrominoBodyOnScroll()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(3, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[] {CellColor.Red, CellColor.Orange, null, CellColor.Yellow,
                CellColor.Green, CellColor.PaleBlue, CellColor.Blue, CellColor.Magenta },
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(4));
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(.4f));

        var eliminatedCells = eliminatedCellsCollecter.Items.Select(c => c.Position).ToArray();
        Assert.True(new Vector2Int[] {
            v2i(4, 0), v2i(4, 1), v2i(4, 3), v2i(4, 4), v2i(4, 5), v2i(4, 6), v2i(4, 7)
        }.HasSameContent(eliminatedCells));
    }

    [Test]
    public void TestFreezingParticleOnBorder()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new RotateEvent(true));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Cell> newCells = newCellsCollector.Items.ToArray();
        Assert.True((new Cell[] { new Cell(v2i(1, 0), CellColor.Green) }).HasSameContent(newCells));
    }

    [Test]
    public void TestShootingAtBorder()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(0, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));
        // One cell on the table
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.HorizontalInput.Right)));
        eventProvider.Publisher.OnNext(new RotateEvent(true));
        eventProvider.Publisher.OnNext(new MotionEvent(new MovementInput(MovementInput.VerticalInput.Down)));
        eventProvider.Publisher.OnNext(new ShotEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<Cell> newCells = newCellsCollector.Items.ToArray();
        Assert.AreEqual(0, newCells.Count());
    }

    [Test]
    public void TestGameOverByNewColumnScroll()
    {
        setUpGame(w: 8, h: 8, playerLocation: v2i(6, 1), scrollTime: 1, projectileSpeed: 8);
        setUpCellColumns(new CellColor?[][] {
            new CellColor?[]{null, null, CellColor.Red, null, null, null, null, null},
            emptyWall8, emptyWall8, emptyWall8, emptyWall8, emptyWall8
        });

        // Imitation
        eventProvider.Publisher.OnNext(new StartNewGameEvent());
        eventProvider.Publisher.OnNext(new FrameUpdateEvent(1));

        IEnumerable<GamePhase> collectedPhases = gamePhaseCollector.Items.ToArray();
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
        eventProvider = new TestEventProvider();
        cellGeneratorMock = new Mock<ICellGenerator>();
        ICellGenerator testCellGenerator = cellGeneratorMock.Object;
        gameLogic = new GameLogic(gameSettings, eventProvider, new TetrominoPatterns(), testCellGenerator);

        gamePhaseCollector = new StreamItemCollector<GamePhase>();
        gameLogic.GamePhaseStream.Subscribe(gamePhaseCollector);

        projectilesCollector = new();
        gameLogic.ProjectileCoordinatesStream.Subscribe(projectilesCollector);

        eliminatedCellsCollecter = new();
        gameLogic.EliminatedBricksStream.Subscribe(eliminatedCellsCollecter);

        plTetromonoCollector = new StreamItemCollector<PlayerTetromino>();
        gameLogic.PlayerTetrominoStream.Subscribe(plTetromonoCollector);
        gameOverCollector = new StreamItemCollector<Unit>();
        gameLogic.GamePhaseStream.Where(phase => phase is GamePhase.GameOver).Select(phase => Unit.Default)
            .Subscribe(gameOverCollector);
        newCellsCollector = new StreamItemCollector<Cell>();
        gameLogic.NewCellStream.Subscribe(newCellsCollector);
    }

    // Mocks cell columns generation
    private void setUpCellColumns(CellColor?[][] walls)
    {
        int wallCounter = 0;
        cellGeneratorMock.Setup(g => g.GenerateCells(It.IsAny<CellColor?[]>()))
            .Callback<CellColor?[]>((buffer) => Array.Copy(walls[wallCounter++], buffer, buffer.Length));
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
