using NUnit.Framework;
using System;
using System.Collections.Generic;
using Tetra4bica.Core;
using Tetra4bica.Util;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

public class ColorTableTest {


    Vector2Int[] matchedCellsBuffer = new Vector2Int[16];

    [Test]
    public void TestEqualsEmpty() {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        ColorTable table2 = new ColorTable(3, 3);
        Assert.AreEqual(table1, table2);
    }

    [Test]
    public void TestEqualsNonEmpty() {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(v2i(0, 1), CellColor.Yellow);
        table1.SetCell(v2i(1, 1), CellColor.Green);
        ColorTable table2 = new ColorTable(3, 3);
        table2.SetCell(v2i(1, 1), CellColor.Green);
        table2.SetCell(v2i(0, 1), CellColor.Yellow);
        Assert.AreEqual(table1, table2);
    }

    [Test]
    public void TestSetCellGetCell() {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(v2i(1, 1), CellColor.Blue);
        table1[2, 0] = CellColor.Green;
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(2, 0)], CellColor.Green);
        Assert.AreEqual(table1[v2i(0, 0)], CellColor.NONE);
    }

    [Test]
    public void TestRemoveCell() {
        ColorTable table1 = new ColorTable(3, 3);
        table1[1, 0] = CellColor.Blue;
        table1.SetCell(v2i(1, 1), CellColor.Blue);
        table1.SetCell(v2i(1, 2), CellColor.Blue);
        table1[1, 2] = CellColor.NONE;
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(1, 0)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(1, 2)], CellColor.NONE);
        table1[1, 0] = CellColor.NONE;
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(1, 0)], CellColor.NONE);
        Assert.AreEqual(table1[v2i(1, 2)], CellColor.NONE);
        table1[1, 1] = CellColor.NONE;
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.NONE);
        Assert.AreEqual(table1[v2i(1, 0)], CellColor.NONE);
        Assert.AreEqual(table1[v2i(1, 2)], CellColor.NONE);
    }

    [Test]
    public void TestSetCellGetCellOutOfBoundsException() {
        ColorTable table1 = new ColorTable(3, 3);
        Assert.That(() => table1[3, 3] = CellColor.Green, Throws.TypeOf<IndexOutOfRangeException>());
    }

    // A Test behaves as an ordinary method
    [Test]
    public void TestFindYellowPatternRectExactSize() {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow);
        table1.SetCell(Vector2Int.right, CellColor.Yellow);
        table1.SetCell(Vector2Int.up, CellColor.Yellow);
        table1.SetCell(Vector2Int.one, CellColor.Yellow);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindYellowPatternRectLargeRegion() {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow);
        table1.SetCell(Vector2Int.right, CellColor.Yellow);
        table1.SetCell(Vector2Int.up, CellColor.Yellow);
        table1.SetCell(Vector2Int.one, CellColor.Yellow);
        table1.SetCell(Vector2Int.up * 2, CellColor.Yellow);
        table1.SetCell(Vector2Int.one + Vector2Int.up, CellColor.Yellow);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindYellowPatternRectLargeRegionGotFromTableItself() {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow);
        table1.SetCell(Vector2Int.right, CellColor.Yellow);
        table1.SetCell(Vector2Int.up, CellColor.Yellow);
        table1.SetCell(Vector2Int.one, CellColor.Yellow);
        table1.SetCell(Vector2Int.up * 2, CellColor.Yellow);
        table1.SetCell(Vector2Int.one + Vector2Int.up, CellColor.Yellow);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }


    [Test]
    public void TestFindGreenPatternStickHorizontalRegionGotFromTableItself() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(Vector2Int.zero, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.right, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.right * 2, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.right * 3, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.right * 4, CellColor.PaleBlue);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindGreenPatternStickVerticalRegionGotFromTableItself() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(Vector2Int.zero, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.up, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.up * 2, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.up * 3, CellColor.PaleBlue);
        table1.SetCell(Vector2Int.up * 4, CellColor.PaleBlue);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.up, matchedCellsBuffer));
    }

    [Test]
    public void TestFindPurpleTPatterStraight() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 1), CellColor.Magenta);
        table1.SetCell(v2i(2, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 0), CellColor.Magenta);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindPurpleTPatterWithNoTOnTable() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 1), CellColor.Magenta);
        table1.SetCell(v2i(2, 1), CellColor.Magenta);
        table1.SetCell(v2i(2, 0), CellColor.Magenta);

        Assert.AreEqual(0, table1.FindPattern(new TetrominoPatterns(), v2i(2, 0), matchedCellsBuffer));
    }

    [Test]
    public void TestFindPurpleTPatter90Clockwise() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 2), CellColor.Magenta);
        table1.SetCell(v2i(1, 0), CellColor.Magenta);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindPurpleTPatter90CounterClockwise() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta);
        table1.SetCell(v2i(1, 1), CellColor.Magenta);
        table1.SetCell(v2i(0, 2), CellColor.Magenta);
        table1.SetCell(v2i(0, 0), CellColor.Magenta);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1), matchedCellsBuffer));
    }

    [Test]
    public void TestFindPurpleTPatter180() {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 0), CellColor.Magenta);
        table1.SetCell(v2i(1, 1), CellColor.Magenta);
        table1.SetCell(v2i(0, 1), CellColor.Magenta);
        table1.SetCell(v2i(0, 2), CellColor.Magenta);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1), matchedCellsBuffer));
    }


    [Test]
    public void TestFindOrangeLPatternWithNoLOnTable() {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1), CellColor.Orange);
        table1.SetCell(v2i(1, 1), CellColor.Orange);
        table1.SetCell(v2i(2, 1), CellColor.Orange);
        table1.SetCell(v2i(2, 0), CellColor.Orange);

        Assert.AreEqual(0, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, matchedCellsBuffer));
    }

    [Test]
    public void TestFindOrangeLPatternCounterClockwise90() {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(1, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(2, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(2, 2) + shift, CellColor.Orange);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1) + shift, matchedCellsBuffer));
    }

    [Test]
    public void TestFindOrangeLPatternClockwise90() {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(1, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(2, 1) + shift, CellColor.Orange);
        table1.SetCell(v2i(0, 0) + shift, CellColor.Orange);

        TestPatterns patterns = new TestPatterns();
        var L = new bool[,] {
            { true, true, true },
            { true, false, false }
        };
        patterns.Add(CellColor.Orange, MatrixUtil.RotateBy90(L, true));
        Assert.AreEqual(4, table1.FindPattern(patterns, v2i(0, 1) + shift, matchedCellsBuffer));
    }


    [Test]
    public void TestScrollWithNoWall() {
        ColorTable table1 = new ColorTable(2, 2);
        table1.SetCell(v2i(0, 0), CellColor.Red);
        table1.SetCell(v2i(1, 1), CellColor.Orange);
        table1.ScrollLeft(new CellColor[] { CellColor.NONE, CellColor.NONE });

        Assert.AreEqual(CellColor.NONE, table1[0, 0]);
        Assert.AreEqual(CellColor.Orange, table1[0, 1]);
        Assert.AreEqual(CellColor.NONE, table1[1, 0]);
        Assert.AreEqual(CellColor.NONE, table1[1, 1]);
    }


    [Test]
    public void TestScrollRainbowWithNoWall() {
        ColorTable table1 = new ColorTable(7, 7);
        table1.SetCell(v2i(0, 0), CellColor.Red);
        table1.SetCell(v2i(1, 1), CellColor.Orange);
        table1.SetCell(v2i(1, 1) * 2, CellColor.Yellow);
        table1.SetCell(v2i(1, 1) * 3, CellColor.Green);
        table1.SetCell(v2i(1, 1) * 4, CellColor.PaleBlue);
        table1.SetCell(v2i(1, 1) * 5, CellColor.Blue);
        table1.SetCell(v2i(1, 1) * 6, CellColor.Magenta);
        table1.ScrollLeft(new CellColor[]{
            CellColor.NONE,
            CellColor.NONE,
            CellColor.NONE,
            CellColor.NONE,
            CellColor.NONE,
            CellColor.NONE,
            CellColor.NONE});

        Assert.AreEqual(CellColor.NONE, table1[0, 0]);
        Assert.AreEqual(CellColor.Orange, table1[0, 1]);
        Assert.AreEqual(CellColor.Yellow, table1[1, 2]);
        Assert.AreEqual(CellColor.Green, table1[2, 3]);
        Assert.AreEqual(CellColor.PaleBlue, table1[3, 4]);
        Assert.AreEqual(CellColor.Blue, table1[4, 5]);
        Assert.AreEqual(CellColor.Magenta, table1[5, 6]);

        Assert.AreEqual(CellColor.NONE, table1[6, 0]);
        Assert.AreEqual(CellColor.NONE, table1[6, 1]);
        Assert.AreEqual(CellColor.NONE, table1[6, 2]);
        Assert.AreEqual(CellColor.NONE, table1[6, 3]);
        Assert.AreEqual(CellColor.NONE, table1[6, 4]);
        Assert.AreEqual(CellColor.NONE, table1[6, 5]);
        Assert.AreEqual(CellColor.NONE, table1[6, 6]);
    }

    [Test]
    public void TestScrollRainbowWithNewWall() {
        ColorTable table1 = new ColorTable(7, 7);
        table1.SetCell(v2i(0, 0), CellColor.Red);
        table1.SetCell(v2i(1, 1), CellColor.Orange);
        table1.SetCell(v2i(2, 2), CellColor.Yellow);
        table1.SetCell(v2i(3, 3), CellColor.Green);
        table1.SetCell(v2i(4, 4), CellColor.PaleBlue);
        table1.SetCell(v2i(5, 5), CellColor.Blue);
        table1.SetCell(v2i(6, 6), CellColor.Magenta);
        table1.ScrollLeft(new CellColor[]{
            CellColor.Red,
            CellColor.Green,
            CellColor.Blue,
            CellColor.PaleBlue,
            CellColor.Magenta,
            CellColor.Yellow,
            CellColor.Orange});

        Assert.AreEqual(CellColor.NONE, table1[0, 0]);
        Assert.AreEqual(CellColor.Orange, table1[0, 1]);
        Assert.AreEqual(CellColor.Yellow, table1[1, 2]);
        Assert.AreEqual(CellColor.Green, table1[2, 3]);
        Assert.AreEqual(CellColor.PaleBlue, table1[3, 4]);
        Assert.AreEqual(CellColor.Blue, table1[4, 5]);
        Assert.AreEqual(CellColor.Magenta, table1[5, 6]);

        Assert.AreEqual(CellColor.Red, table1[6, 0]);
        Assert.AreEqual(CellColor.Green, table1[6, 1]);
        Assert.AreEqual(CellColor.Blue, table1[6, 2]);
        Assert.AreEqual(CellColor.PaleBlue, table1[6, 3]);
        Assert.AreEqual(CellColor.Magenta, table1[6, 4]);
        Assert.AreEqual(CellColor.Yellow, table1[6, 5]);
        Assert.AreEqual(CellColor.Orange, table1[6, 6]);
    }


    class TestPatterns : ICellPatterns {

        Dictionary<CellColor, bool[,]> patternMap = new Dictionary<CellColor, bool[,]>();

        public TestPatterns() { }

        public void Add(CellColor color, bool[,] pattern) {
            patternMap[color] = pattern;
        }

        public IEnumerable<bool[,]> GetPatterns(CellColor color) {
            return new[] { patternMap[color] };
        }

        CellFragment[] ICellPatterns.GetPatterns(CellColor color) {
            return new CellFragment[] { CellFragment.Fragment(patternMap[color], out var _) };
        }
    }
}
