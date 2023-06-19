using NUnit.Framework;
using System.Linq;
using Tetra4bica.Core;
using Tetra4bica.Util.StructIterators;
using UnityEngine;
using static Sergei.Safonov.Utility.VectorExt;
using Is = UnityEngine.TestTools.Constraints.Is;

public class CellFragmentTest {

    [Test]
    public void TestFragmentCreationNoShift() {
        var fragment1 = CellFragment.Fragment(new bool[,] { { false, true }, { true, true }, { false, true } }, out var shift);
        Assert.AreEqual(Vector2Int.zero, shift);
        Assert.AreEqual(new Vector2Int[] { v2i(0, 1), v2i(1, 0), v2i(1, 1), v2i(2, 1) }.ToHashSet(), fragment1);
    }

    [Test]
    public void TestFragmentCreationShiftRight() {
        var fragment1 = CellFragment.Fragment(new bool[,] { { false, false }, { true, true }, { false, true } }, out var shift);
        Assert.AreEqual(Vector2Int.right, shift);
        Assert.AreEqual(new Vector2Int[] { v2i(0, 0), v2i(0, 1), v2i(1, 1) }.ToHashSet(), fragment1);
    }

    [Test]
    public void TestFragmentCreationShiftUp() {
        var fragment1 = CellFragment.Fragment(new bool[,] { { false, true }, { false, true }, { false, true } }, out var shift);
        Assert.AreEqual(Vector2Int.up, shift);
        Assert.AreEqual(new Vector2Int[] { v2i(0, 0), v2i(1, 0), v2i(2, 0) }.ToHashSet(), fragment1);
    }

    [Test]
    public void TestFragmentCreationShiftUpRight() {
        var fragment1 = CellFragment.Fragment(new bool[,] { { false, false }, { false, true }, { false, true } }, out var shift);
        Assert.AreEqual(Vector2Int.one, shift);
        Assert.AreEqual(new Vector2Int[] { v2i(0, 0), v2i(1, 0) }.ToHashSet(), fragment1);
    }

    [Test]
    public void TestCreateEqualSingleCellFragments() {
        var fragment1 = CellFragment.Fragment(new bool[,] { { true, false }, { false, false } }, out var _);
        CellFragment fragment2 = CellFragment.Fragment(new bool[,] { { false, false }, { false, true } }, out var _);

        Assert.AreEqual(fragment1, fragment2);
    }


    [Test]
    public void TestCellFragmentEnumeratorNoAllocations() {
        bool[,] fragment1Cells = new bool[,] { { true, false }, { false, false } };

        var fragment1 = CellFragment.Fragment(fragment1Cells, out var _);
        Assert.That(
            () => {
                HashSetVector2IntWrapper iter = fragment1.GetEnumerator();
                iter.MoveNext();
                var f = iter.Current;
            },
            !Is.AllocatingGCMemory()
        );
    }
}
