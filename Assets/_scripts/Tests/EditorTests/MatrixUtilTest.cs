using NUnit.Framework;
using System.Collections.Generic;
using Tetra4bica.Util;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;

public class MatrixUtilTest {

    [Test]
    public void TestRotation90cBoolArray() {
        var m = new bool[,] {
            { false, true },
            { true, true },
            { false, true }
        };
        var mRotated = new bool[,] {
            { false, true, false },
            { true, true, true },
        };
        Assert.AreEqual(mRotated, MatrixUtil.RotateBy90(m, true));
    }

    [Test]
    public void TestRotation90cVector2IntEnumerable() {
        var m = new HashSet<Vector2Int>() { v2i(0, 1), v2i(1, 0), v2i(1, 1), v2i(2, 1) };
        var mExpected = new HashSet<Vector2Int>() { v2i(0, 1), v2i(1, 0), v2i(1, 1), v2i(1, 2) };
        IEnumerable<Vector2Int> mRotated = MatrixUtil.RotateBy90(m, true);
        Assert.True(mExpected.SetEquals(mRotated));
    }

    [Test]
    public void TestRotation90ccBoolArray() {
        var m = new bool[,] {
            { false, true },
            { true, true },
            { false, true }
        };
        var mRotated = new bool[,] {
            { true, true, true },
            { false, true, false },
        };
        Assert.AreEqual(mRotated, MatrixUtil.RotateBy90(m, false));
    }

    [Test]
    public void TestRotation90ccVector2IntEnumerable() {
        var m = new HashSet<Vector2Int>() { v2i(0, 1), v2i(1, 0), v2i(1, 1), v2i(2, 1) };
        var mExpected = new HashSet<Vector2Int>() { v2i(0, 0), v2i(0, 1), v2i(0, 2), v2i(1, 1) };
        IEnumerable<Vector2Int> mRotated = MatrixUtil.RotateBy90(m, false);
        Assert.True(mExpected.SetEquals(mRotated));
    }

    [Test]
    public void TestRotation180BoolArray() {
        var m = new bool[,] {
            { false, true },
            { true, true },
            { false, true }
        };
        var mRotated = new bool[,] {
            { true, false },
            { true, true },
            { true, false }
        };
        Assert.AreEqual(mRotated, MatrixUtil.RotateBy180(m));
    }
}
