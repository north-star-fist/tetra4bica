using NUnit.Framework;
using Tetra4bica.Core;
using static Sergei.Safonov.Utility.VectorExt;

public class CellTest {

    [Test]
    public void TestEqual() {
        Cell cell1 = new Cell(v2i(1, 2), CellColor.Red);
        Cell cell2 = new Cell(v2i(1, 2), CellColor.Red);
        Assert.AreEqual(cell1, cell2);
        Assert.True(cell1 == cell2);
        Assert.True(cell1.Equals((object)cell2));
    }

    [Test]
    public void TestNotEqual() {
        Cell cell1 = new Cell(v2i(1, 2), CellColor.Red);
        Cell cell2 = new Cell(v2i(2, 2), CellColor.Red);
        Assert.AreNotEqual(cell1, cell2);
        Assert.True(cell1 != cell2);
        Assert.False(cell1.Equals((object)cell2));

        Cell cell3 = new Cell(v2i(1, 2), CellColor.Red);
        Cell cell4 = new Cell(v2i(1, 2), CellColor.Magenta);
        Assert.AreNotEqual(cell3, cell4);
        Assert.True(cell3 != cell4);
        Assert.False(cell3.Equals((object)cell4));

        Assert.False(cell1.Equals(new object()));
    }

    [Test]
    public void TestHashCodeOfEqualCells() {
        Cell cell1 = new Cell(v2i(1, 2), CellColor.Red);
        Cell cell2 = new Cell(v2i(1, 2), CellColor.Red);
        Assert.AreEqual(cell1.GetHashCode(), cell2.GetHashCode());
    }
}
