using NUnit.Framework;
using System.Collections.Generic;
using UniRx;

public class UniRxTest {

    static int appendix = 0;

    [Test]
    public void TestUniRxSelectWithMultipleSubscriptions() {
        var subj = new Subject<int>();
        var selectObs = subj.Select(i => i + appendix++).Share();

        var collection1 = new List<int>();
        selectObs.Subscribe(i => { collection1.Add(i); });
        var collection2 = new List<int>();
        selectObs.Subscribe(i => { collection2.Add(i + 1); });

        subj.OnNext(1);
        subj.OnNext(2);
        subj.OnNext(3);

        Assert.True(collection1.Count == 3);
        Assert.True(collection2.Count == 3);

        Assert.True(collection1.Contains(1));
        Assert.True(collection1.Contains(3));
        Assert.True(collection1.Contains(5));

        Assert.True(collection2.Contains(2));
        Assert.True(collection2.Contains(4));
        Assert.True(collection2.Contains(6));
    }
}
