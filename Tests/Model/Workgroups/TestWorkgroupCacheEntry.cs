using Thetacat.Model.Workgroups;
using Thetacat.Util;

namespace Tests.Model.Workgroups;

public class TestWorkgroupCacheEntry
{
    [Test]
    public static void TestMakeUpdatePairs_NoBase()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(0, updates.Count);
    }

    [Test]
    public static void TestMakeUpdatePairs_BaseIdentical()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        entry.Path = new PathSegment("/foobar/foo.jpg"); // same value

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(0, updates.Count);
    }

    [Test]
    public static void TestMakeUpdatePairs_PathChanged()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        entry.Path = new PathSegment("/foobar/foo1.jpg"); // same value

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("path", updates[0].Key);
        Assert.AreEqual("'/foobar/foo1.jpg'", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_CachedByChanged()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        entry.CachedBy = Guid.Parse("10000000-0000-0000-0000-000000000002");

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("cachedBy", updates[0].Key);
        Assert.AreEqual("'10000000-0000-0000-0000-000000000002'", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_CachedDateFromNullChanged()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        entry.CachedDate = DateTime.Parse("8/21/1993");

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("cachedDate", updates[0].Key);
        Assert.AreEqual($"'{entry.CachedDate.ToString()}'", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_CachedDateToNullChanged()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            DateTime.Parse("8/21/1993"),
            true,
            null);

        entry.CachedDate = null;

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("cachedDate", updates[0].Key);
        Assert.AreEqual($"null", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_VectorClockChangedFromNull()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            null);

        entry.VectorClock = 1;

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("vectorClock", updates[0].Key);
        Assert.AreEqual("1", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_VectorClockChangedToNull()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            1);

        entry.VectorClock = null;

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(1, updates.Count);
        Assert.AreEqual("vectorClock", updates[0].Key);
        Assert.AreEqual("null", updates[0].Value);
    }

    [Test]
    public static void TestMakeUpdatePairs_VectorClockChangedToNullAndCachedDateChangeFromNull()
    {
        WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000000"),
            new PathSegment("/foobar/foo.jpg"),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            null,
            true,
            1);

        entry.VectorClock = null;
        entry.CachedDate = DateTime.Parse("8/21/1993");

        List<KeyValuePair<string, string>> updates = entry.MakeUpdatePairs();

        Assert.AreEqual(2, updates.Count);
        Assert.AreEqual("cachedDate", updates[0].Key);
        Assert.AreEqual($"'{entry.CachedDate.ToString()}'", updates[0].Value);
        Assert.AreEqual("vectorClock", updates[1].Key);
        Assert.AreEqual("null", updates[1].Value);
    }
}
