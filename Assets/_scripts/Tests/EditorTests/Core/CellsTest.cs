using NUnit.Framework;
using Tetra4bica.Core;
using UnityEngine;

public class CellsTest {

    [Test]
    public void TestCellColorToUnityColor() {
        Assert.True(Color.black == Cells.ToUnityColor(CellColor.NONE));
        Assert.True(Color.red == Cells.ToUnityColor(CellColor.Red));
        Assert.True(Color.cyan == Cells.ToUnityColor(CellColor.PaleBlue));
        Assert.True(Color.blue == Cells.ToUnityColor(CellColor.Blue));
        Assert.True(Color.magenta == Cells.ToUnityColor(CellColor.Magenta));
        Assert.True(Color.yellow == Cells.ToUnityColor(CellColor.Yellow));
        Assert.True(Color.Lerp(Color.yellow, Color.red, 0.5f) == Cells.ToUnityColor(CellColor.Orange));
        Assert.True(Color.green == Cells.ToUnityColor(CellColor.Green));
    }
}
