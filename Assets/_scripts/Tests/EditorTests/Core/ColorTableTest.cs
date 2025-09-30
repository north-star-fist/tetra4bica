using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tetra4bica.Core;
using Tetra4bica.Util;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

public class ColorTableTest
{

    private readonly Vector2Int[] _matchedCellsBuffer = new Vector2Int[16];

    [Test]
    public void TestEqualsEmpty()
    {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        ColorTable table2 = new ColorTable(3, 3);
        Assert.AreEqual(table1, table2);
    }

    [Test]
    public void TestEqualsNonEmpty()
    {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(v2i(0, 1), CellColor.Yellow, null);
        table1.SetCell(v2i(1, 1), CellColor.Green, null);
        ColorTable table2 = new ColorTable(3, 3);
        table2.SetCell(v2i(1, 1), CellColor.Green, null);
        table2.SetCell(v2i(0, 1), CellColor.Yellow, null);
        Assert.AreEqual(table1, table2);
    }

    [Test]
    public void TestSetCellGetCell()
    {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(v2i(1, 1), CellColor.Blue, null);
        table1.SetCell(2, 0, CellColor.Green, null);
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(2, 0)], CellColor.Green);
        Assert.IsFalse(table1[v2i(0, 0)].HasValue);
    }

    [Test]
    public void TestRemoveCell()
    {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(1, 0, CellColor.Blue, null);
        table1.SetCell(v2i(1, 1), CellColor.Blue, null);
        table1.SetCell(v2i(1, 2), CellColor.Blue, null);
        table1.SetCell(1, 2, null, null);
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.AreEqual(table1[v2i(1, 0)], CellColor.Blue);
        Assert.IsFalse(table1[v2i(1, 2)].HasValue);
        table1.SetCell(1, 0, null, null);
        Assert.AreEqual(table1[v2i(1, 1)], CellColor.Blue);
        Assert.IsFalse(table1[v2i(1, 0)].HasValue);
        Assert.IsFalse(table1[v2i(1, 2)].HasValue);
        table1.SetCell(1, 1, null, null);
        Assert.IsFalse(table1[v2i(1, 1)].HasValue);
        Assert.IsFalse(table1[v2i(1, 0)].HasValue);
        Assert.IsFalse(table1[v2i(1, 2)].HasValue);
    }

    [Test]
    public void TestSetCellGetCellOutOfBoundsException()
    {
        ColorTable table1 = new ColorTable(3, 3);
        Assert.That(() => table1.SetCell(3, 3,  CellColor.Green, null), Throws.TypeOf<IndexOutOfRangeException>());
    }

    // A Test behaves as an ordinary method
    [Test]
    public void TestFindYellowPatternRectExactSize()
    {
        // Use the Assert class to test conditions
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.right, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.up, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.one, CellColor.Yellow, null);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Yellow, pCol);
    }

    [Test]
    public void TestFindYellowPatternRectLargeRegion()
    {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.right, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.up, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.one, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.up * 2, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.one + Vector2Int.up, CellColor.Yellow, null);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Yellow, pCol);
    }

    [Test]
    public void TestFindYellowPatternRectLargeRegionGotFromTableItself()
    {
        ColorTable table1 = new ColorTable(3, 3);
        table1.SetCell(Vector2Int.zero, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.right, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.up, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.one, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.up * 2, CellColor.Yellow, null);
        table1.SetCell(Vector2Int.one + Vector2Int.up, CellColor.Yellow, null);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Yellow, pCol);
    }


    [Test]
    public void TestFindPaleBluePatternStickHorizontalRegionGotFromTableItself()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(Vector2Int.zero, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.right, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.right * 2, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.right * 3, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.right * 4, CellColor.PaleBlue, null);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.PaleBlue, pCol);
    }

    [Test]
    public void TestFindPaleBluePatternStickVerticalRegionGotFromTableItself()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(Vector2Int.zero, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.up, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.up * 2, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.up * 3, CellColor.PaleBlue, null);
        table1.SetCell(Vector2Int.up * 4, CellColor.PaleBlue, null);
        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.up, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.PaleBlue, pCol);
    }

    [Test]
    public void TestFindMagentaTPatterStraight()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(2, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 0), CellColor.Magenta, null);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Magenta, pCol);
    }

    [Test]
    public void TestFindMagentaTPatterWithNoTOnTable()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(2, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(2, 0), CellColor.Magenta, null);

        Assert.AreEqual(0, table1.FindPattern(new TetrominoPatterns(), v2i(2, 0), _matchedCellsBuffer, out var pCol));
        Assert.IsFalse(pCol.HasValue);
    }

    [Test]
    public void TestFindMagentaTPatter90Clockwise()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 2), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 0), CellColor.Magenta, null);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), Vector2Int.right, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Magenta, pCol);
    }

    [Test]
    public void TestFindMagentaTPatter90CounterClockwise()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(0, 2), CellColor.Magenta, null);
        table1.SetCell(v2i(0, 0), CellColor.Magenta, null);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1), _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Magenta, pCol);
    }

    [Test]
    public void TestFindMagentaTPatter180()
    {
        ColorTable table1 = new ColorTable(5, 5);
        table1.SetCell(v2i(0, 0), CellColor.Magenta, null);
        table1.SetCell(v2i(1, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(0, 1), CellColor.Magenta, null);
        table1.SetCell(v2i(0, 2), CellColor.Magenta, null);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1), _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Magenta, pCol);
    }


    [Test]
    public void TestFindOrangeLPatternWithNoLOnTable()
    {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1), CellColor.Orange, null);
        table1.SetCell(v2i(1, 1), CellColor.Orange, null);
        table1.SetCell(v2i(2, 1), CellColor.Orange, null);
        table1.SetCell(v2i(2, 0), CellColor.Orange, null);

        Assert.AreEqual(0, table1.FindPattern(
            new TetrominoPatterns(),
            Vector2Int.right,
            _matchedCellsBuffer,
            out var pCol)
        );
        Assert.IsFalse(pCol.HasValue);
    }

    [Test]
    public void TestFindOrangeLPatternCounterClockwise90()
    {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(1, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(2, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(2, 2) + shift, CellColor.Orange, null);

        Assert.AreEqual(4, table1.FindPattern(new TetrominoPatterns(), v2i(1, 1) + shift, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Orange, pCol);
    }

    [Test]
    public void TestFindOrangeLPatternClockwise90()
    {
        var shift = v2i(2, 3);
        ColorTable table1 = new ColorTable(10, 10);
        table1.SetCell(v2i(0, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(1, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(2, 1) + shift, CellColor.Orange, null);
        table1.SetCell(v2i(0, 0) + shift, CellColor.Orange, null);

        TestPatterns patterns = new TestPatterns();
        var l = new bool[,] {
            { true, true, true },
            { true, false, false }
        };
        patterns.Add(CellColor.Orange, MatrixUtil.RotateBy90(l, true));
        Assert.AreEqual(4, table1.FindPattern(patterns, v2i(0, 1) + shift, _matchedCellsBuffer, out var pCol));
        Assert.AreEqual(CellColor.Orange, pCol);
    }


    [Test]
    public void TestScrollWithNoWall()
    {
        ColorTable table1 = new ColorTable(2, 2);
        table1.SetCell(v2i(0, 0), CellColor.Red, null);
        table1.SetCell(v2i(1, 1), CellColor.Orange, null);
        table1.ScrollLeft(new CellColor?[] { null, null });

        Assert.IsFalse(table1[0, 0].HasValue);
        Assert.AreEqual(CellColor.Orange, table1[0, 1]);
        Assert.IsFalse(table1[1, 0].HasValue);
        Assert.IsFalse(table1[1, 1].HasValue);
    }


    [Test]
    public void TestScrollRainbowWithNoWall()
    {
        ColorTable table1 = new ColorTable(7, 7);
        table1.SetCell(v2i(0, 0), CellColor.Red, null);
        table1.SetCell(v2i(1, 1), CellColor.Orange, null);
        table1.SetCell(v2i(1, 1) * 2, CellColor.Yellow, null);
        table1.SetCell(v2i(1, 1) * 3, CellColor.Green, null);
        table1.SetCell(v2i(1, 1) * 4, CellColor.PaleBlue, null);
        table1.SetCell(v2i(1, 1) * 5, CellColor.Blue, null);
        table1.SetCell(v2i(1, 1) * 6, CellColor.Magenta, null);
        table1.ScrollLeft(new CellColor?[] { null, null, null, null, null, null, null });

        Assert.IsFalse(table1[0, 0].HasValue);
        Assert.AreEqual(CellColor.Orange, table1[0, 1]);
        Assert.AreEqual(CellColor.Yellow, table1[1, 2]);
        Assert.AreEqual(CellColor.Green, table1[2, 3]);
        Assert.AreEqual(CellColor.PaleBlue, table1[3, 4]);
        Assert.AreEqual(CellColor.Blue, table1[4, 5]);
        Assert.AreEqual(CellColor.Magenta, table1[5, 6]);

        Assert.IsFalse(table1[6, 0].HasValue);
        Assert.IsFalse(table1[6, 1].HasValue);
        Assert.IsFalse(table1[6, 2].HasValue);
        Assert.IsFalse(table1[6, 3].HasValue);
        Assert.IsFalse(table1[6, 4].HasValue);
        Assert.IsFalse(table1[6, 5].HasValue);
        Assert.IsFalse(table1[6, 6].HasValue);
    }

    [Test]
    public void TestScrollRainbowWithNewWall()
    {
        ColorTable table1 = new ColorTable(7, 7);
        table1.SetCell(v2i(0, 0), CellColor.Red, null);
        table1.SetCell(v2i(1, 1), CellColor.Orange, null);
        table1.SetCell(v2i(2, 2), CellColor.Yellow, null);
        table1.SetCell(v2i(3, 3), CellColor.Green, null);
        table1.SetCell(v2i(4, 4), CellColor.PaleBlue, null);
        table1.SetCell(v2i(5, 5), CellColor.Blue, null);
        table1.SetCell(v2i(6, 6), CellColor.Magenta, null);
        table1.ScrollLeft(new CellColor?[]{
            CellColor.Red,
            CellColor.Green,
            CellColor.Blue,
            CellColor.PaleBlue,
            CellColor.Magenta,
            CellColor.Yellow,
            CellColor.Orange});

        Assert.IsFalse(table1[0, 0].HasValue);
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


    class TestPatterns : ICellPatterns
    {

        private readonly Dictionary<CellColor, bool[,]> _patternMap = new Dictionary<CellColor, bool[,]>();

        public TestPatterns() { }

        public void Add(CellColor color, bool[,] pattern)
        {
            _patternMap[color] = pattern;
        }

        public IEnumerable<bool[,]> GetPatterns(CellColor color)
        {
            return new[] { _patternMap[color] };
        }

        CellFragment[] ICellPatterns.GetPatterns(CellColor color)
        {
            return new CellFragment[] { CellFragment.Fragment(_patternMap[color], out var _) };
        }
    }
}
