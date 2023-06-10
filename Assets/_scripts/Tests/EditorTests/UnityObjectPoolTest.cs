using NUnit.Framework;
using UnityEngine.Pool;

public class UnityObjectPoolTest {

    [Test]
    public void TestOverMaximumBorrowing() {
        var pool = new ObjectPool<object>(create, onGet, onRelease, onDestroy, false, 1, 2);

        Assert.AreEqual(0, pool.CountAll);
        Assert.AreEqual(0, pool.CountActive);

        var obj1 = pool.Get();
        Assert.AreEqual(1, pool.CountAll);
        Assert.AreEqual(1, pool.CountActive);

        var obj2 = pool.Get();
        Assert.AreEqual(2, pool.CountAll);
        Assert.AreEqual(2, pool.CountActive);

        var obj3 = pool.Get();
        Assert.AreEqual(3, pool.CountAll);
        Assert.AreEqual(3, pool.CountActive);

        var obj4 = pool.Get();
        Assert.AreEqual(4, pool.CountAll);
        Assert.AreEqual(4, pool.CountActive);

        Assert.IsNotNull(obj3);
        Assert.IsNotNull(obj4);
    }

    private void onDestroy(object obj) { }

    private void onRelease(object obj) { }

    private void onGet(object obj) { }
    private object create() => new object();
}
