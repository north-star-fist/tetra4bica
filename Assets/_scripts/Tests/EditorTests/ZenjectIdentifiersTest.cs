using NUnit.Framework;
using Zenject;

[TestFixture]
public class ZenjectIdentifiersTest : ZenjectUnitTestFixture
{

    interface ITest { }

    class Test : ITest { }

    const string ID_INSTANCED_1 = "I1";
    const string ID_INSTANCED_2 = "I2";

    const string ID_BY_METHOD_1 = "M1";
    const string ID_BY_METHOD_2 = "M2";

    static ITest s_instance1 = new Test();
    static ITest s_instance2 = new Test();

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        Container.Bind<ITest>().WithId(ID_INSTANCED_1).FromInstance(s_instance1).AsCached().NonLazy();
        Container.Bind<ITest>().WithId(ID_INSTANCED_2).FromInstance(s_instance2).AsCached().NonLazy();

        Container.Bind<ITest>().WithId(ID_BY_METHOD_1).FromMethod(geneate1).AsCached().NonLazy();
        Container.Bind<ITest>().WithId(ID_BY_METHOD_2).FromMethod(geneate1).AsCached().NonLazy();
        //Container.Bind<ITest>().WithId(ID_BY_METHOD_2).FromMethod(geneate1).AsCached().NonLazy();
    }

    private ITest geneate1()
    {
        return new Test();
    }

    /*[Test]
    public void TestDefault() {
        var obj1 = Container.Resolve<ITest>();

        Assert.AreEqual(obj1, defaultOne);
    }*/

    [Test]
    public void TestNoDefaults()
    {
        Assert.Throws<Zenject.ZenjectException>(() => Container.Resolve<ITest>());
    }

    [Test]
    public void TestInstance()
    {
        var obj1 = Container.ResolveId<ITest>(ID_INSTANCED_1);
        var obj2 = Container.ResolveId<ITest>(ID_INSTANCED_1);

        Assert.AreEqual(s_instance1, obj1);
        Assert.AreNotEqual(s_instance2, obj1);

        Assert.AreEqual(s_instance1, obj2);
        Assert.AreNotEqual(s_instance2, obj2);
    }

    [Test]
    public void TestMethod()
    {
        var obj1 = Container.ResolveId<ITest>(ID_BY_METHOD_1);
        var obj2 = Container.ResolveId<ITest>(ID_BY_METHOD_1);

        var obj3 = Container.ResolveId<ITest>(ID_BY_METHOD_2);
        var obj4 = Container.ResolveId<ITest>(ID_BY_METHOD_2);

        Assert.AreEqual(obj1, obj2);
        Assert.AreEqual(obj3, obj4);

        Assert.AreNotEqual(obj1, obj3);
        Assert.AreNotEqual(obj2, obj4);
    }
}
