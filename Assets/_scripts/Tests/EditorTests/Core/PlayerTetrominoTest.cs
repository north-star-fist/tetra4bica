using NUnit.Framework;
using Sergei.Safonov.Utility;
using System.Collections.Generic;
using System.Linq;
using Tetra4bica.Core;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

public class PlayerTetrominoTest {

    [Test]
    public void TestConstruction() {
        Vector2Int pos = v2i(3, 4);
        var tetromino = new PlayerTetromino(pos, CellColor.Orange);
        Assert.AreEqual(pos, tetromino.Position);
        Assert.AreEqual(CellColor.Orange, tetromino.Color);
    }

    [Test]
    public void TestWithPosition() {
        Vector2Int pos = v2i(3, 4);
        Vector2Int shift = v2i(-5, 30);
        var tetromino = new PlayerTetromino(pos, CellColor.Orange);
        var shiftedTetromino = tetromino.WithPosition(pos + shift);
        Assert.AreEqual(pos + shift, shiftedTetromino.Position);
        var expectedTetromino = new PlayerTetromino(pos + shift, CellColor.Orange);
        Assert.AreEqual(expectedTetromino, shiftedTetromino);
    }

    [Test]
    public void TestRotateClockwise() {
        TetrominoPatterns patterns = new TetrominoPatterns();
        var tetromino = new PlayerTetromino(v2i(3, 4), CellColor.Orange);
        var rotatedTetromino = tetromino.Rotate(true);
        var expectedTetromino = new PlayerTetromino(
            v2i(2, 4),
            patterns.TPATTERNS.First(),
            v2i(1, 1),
            v2i(1, -1),
            Vector2Int.down,
            CellColor.Orange
        );
        Assert.AreEqual(expectedTetromino, rotatedTetromino);
    }

    [Test]
    public void TestRotateCounterClockwise() {
        TetrominoPatterns patterns = new TetrominoPatterns();
        var tetromino = new PlayerTetromino(v2i(3, 4), CellColor.Orange);
        var rotatedTetromino = tetromino.Rotate(false);
        var expectedTetromino = new PlayerTetromino(
            position: v2i(2, 5),
           formMatrix: patterns.TPATTERNS.Skip(2).First(),
            pivot: v2i(1, 0),
            muzzle: v2i(1, 2),
            direction: Vector2Int.up,
            playerColor: CellColor.Orange
        );
        Assert.AreEqual(expectedTetromino, rotatedTetromino);
    }

    [Test]
    public void TestRotateClockwiseZeroY() {
        TetrominoPatterns patterns = new TetrominoPatterns();
        var tetromino = new PlayerTetromino(v2i(5, 0), CellColor.Orange);
        var rotatedTetromino = tetromino.Rotate(true);
        var expectedTetromino = new PlayerTetromino(
            v2i(4, 0),
            patterns.TPATTERNS.First(),
            v2i(1, 1),
            v2i(1, -1),
            Vector2Int.down,
            CellColor.Orange
        );
        Assert.AreEqual(expectedTetromino, rotatedTetromino);
    }

    [Test]
    public void TestContains() {
        TetrominoPatterns patterns = new TetrominoPatterns();
        Vector2Int pos = v2i(5, 2);
        var tetromino = new PlayerTetromino(pos, CellColor.Orange);

        Assert.True(tetromino.Contains(pos + v2i(0, 0)));
        Assert.True(tetromino.Contains(pos + v2i(0, 1)));
        Assert.True(tetromino.Contains(pos + v2i(0, 2)));
        Assert.True(tetromino.Contains(pos + v2i(1, 1)));
    }

    [Test]
    public void TestEnumerable() {
        Vector2Int pos = v2i(5, 2);
        var tetromino = new PlayerTetromino(pos, CellColor.Orange);

        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        foreach (var c in tetromino) {
            cells.Add(c);
        }
        Assert.True(new Vector2Int[] { pos + v2i(0, 0), pos + v2i(0, 1), pos + v2i(0, 2), pos + v2i(1, 1) }
        .HasSameContent(cells));
    }
}
