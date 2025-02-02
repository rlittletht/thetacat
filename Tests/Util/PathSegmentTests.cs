using Thetacat.Util;

namespace Tests.Util;

public class PathSegmentTests
{
    [TestCase("noSeparators.ext", "noSeparators.ext", null)]
    [TestCase("\\leadingSeparator.ext", "/leadingSeparator.ext", null)]
    [TestCase("/leadingForwardSlash.ext", "/leadingForwardSlash.ext", null)]
    [TestCase("trailingSlash\\", "trailingSlash/", null)]
    [TestCase("trailingForward/", "trailingForward/", null)]
    [TestCase("\\leadingAndTrailing\\", "/leadingAndTrailing/", null)]
    [TestCase("/leadingAndTrailingForward/", "/leadingAndTrailingForward/", null)]
    [TestCase("\\\\unc\\path\\noSeparators.ext", "//unc/path/noSeparators.ext", null)]
    [Test]
    public static void TestCreateFromString(string path, string expectedSegment, string? expectedLocal)
    {
        PathSegment expected = new PathSegment(expectedSegment, expectedLocal);
        PathSegment actual = PathSegment.CreateFromString(path);

        Assert.AreEqual(expected, actual);
    }

    [TestCase("path1", "path2", "path1/path2", null)]
    [TestCase("path1\\2", "path2", "path1/2/path2", null)]
    [TestCase("path1", "path2\\2", "path1/path2/2", null)]
    [TestCase("path1/", "path2", "path1/path2", null)]
    [TestCase("\\path1", "path2", "/path1/path2", null)]
    [TestCase("\\path1", "\\path2", "/path1/path2", null)]
    [TestCase("/path1", "/path2", "/path1/path2", null)]
    [TestCase("", "/path2", "/path2", null)]
    [TestCase("path1", "", "path1", null)]
    [Test]
    public static void TestPathCombineBoth(string pathA, string pathB, string expectedSegment, string? expectedLocal)
    {
        PathSegment expected = new PathSegment(expectedSegment, expectedLocal);

        PathSegment segmentA = new PathSegment(pathA);
        PathSegment segmentB = new PathSegment(pathB);

        PathSegment actual = PathSegment.Join(segmentA, segmentB);

        Assert.AreEqual(expected, actual);
    }

    [TestCase("foo", "", null)]
    [TestCase("", "", null)]
    [TestCase("/foo", "/", null)]
    [TestCase("\\foo", "/", null)]
    [TestCase("\\\\foo\\bar\\foo", "//foo/bar", null)]
    [TestCase("c:\\foo", "c:/", null)]
    [Test]
    public static void TestGetPathRoot(string path, string expectedSegment, string? expectedLocal)
    {
        PathSegment expected = new PathSegment(expectedSegment, expectedLocal);

        PathSegment actual1 = PathSegment.GetPathRoot(path);
        PathSegment actual2 = PathSegment.GetPathRoot(new PathSegment(path));

        Assert.AreEqual(expected, actual1);
        Assert.AreEqual(expected, actual2);
    }

    [TestCase("foo.txt", "1", "foo1.txt", "foo1.txt")]
    [TestCase("/foo.txt", "1", "/foo1.txt", "\\foo1.txt")]
    [TestCase("foo.txt.jpg", "1", "foo.txt1.jpg", "foo.txt1.jpg")]
    [TestCase("\\foo.txt.jpg", "1", "/foo.txt1.jpg", "\\foo.txt1.jpg")]
    [TestCase("//some/server/root/foo.txt.jpg", "1", "//some/server/root/foo.txt1.jpg", "\\\\some\\server\\root\\foo.txt1.jpg")]
    [TestCase("foo.txt/noextension", "1", "foo.txt/noextension1", "foo.txt\\noextension1")]
    [TestCase("foo.txt/noextension.", "1", "foo.txt/noextension1", "foo.txt\\noextension1")]
    [TestCase("foo.txt/noextension..", "1", "foo.txt/noextension.1", "foo.txt\\noextension.1")]
    [TestCase("foo.txt/noextension..foo", "1", "foo.txt/noextension.1.foo", "foo.txt\\noextension.1.foo")]
    [Test]
    public static void TestAppendLeafSuffix(string path, string suffix, string expectedSegment, string expectedLocal)
    {
        PathSegment start = new PathSegment(path);

        PathSegment expected = new PathSegment(expectedSegment);
        PathSegment actual = start.AppendLeafSuffix(suffix);

        Assert.AreEqual(expected, actual);
        Assert.AreEqual(expected.Local, expectedLocal);
    }

    [TestCase("foo.txt", new string[] { })]
    [TestCase("foo/foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("bar\\foo\\foo.txt", new string[] { "bar/foo/foo.txt", "foo/foo.txt" })]
    [TestCase("c:\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("\\\\baz\\boo\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("\\\\baz\\boo\\foo\\bum\\bun\\foo.txt", new string[] { "foo/bum/bun/foo.txt", "bum/bun/foo.txt", "bun/foo.txt" })]
    [Test]
    public static void TestTraverseDirectories_TraverseAll(string localPath, string[] expected)
    {
        List<string> actual = new();

        PathSegment path = new(localPath);

        path.TraverseDirectories(
            (segment) =>
            {
                actual.Add(segment);
                return true;
            });

        Assert.AreEqual(expected, actual);
    }

    [TestCase("foo.txt", new string[] { })]
    [TestCase("foo/foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("bar\\foo\\foo.txt", new string[] { "bar/foo/foo.txt" })]
    [TestCase("c:\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [TestCase("\\\\baz\\boo\\foo\\foo.txt", new string[] { "foo/foo.txt" })]
    [Test]
    public static void TestTraverseDirectories_TraverseOnlyOne(string localPath, string[] expected)
    {
        List<string> actual = new();

        PathSegment path = new(localPath);
        int c = 0;

        path.TraverseDirectories(
            (segment) =>
            {
                c++;
                actual.Add(segment);
                return c < 1;
            });

        Assert.AreEqual(expected, actual);
    }

    [TestCase("//server/share/foo/bar/foo.txt", "//server/share/foo/bar")]
    [TestCase("foo/bar/foo.txt", "foo/bar")]
    [TestCase("/foo/bar/foo.txt", "/foo/bar")]
    [TestCase("/foo.txt", "/")]
    [TestCase("foo.txt", "")]
    public static void TestGetDirectory(string localPath, string expected)
    {
        PathSegment path = PathSegment.CreateFromString(localPath);
        PathSegment expectedPath = PathSegment.CreateFromString(expected);

        Assert.AreEqual(expectedPath, path.GetPathDirectory());
    }

    [TestCase("\\\\server\\share\\root\\subdir", "\\\\server\\share\\root", true)]
    [TestCase("\\\\server\\share\\root\\subdir", "\\\\server\\share\\root2", false)]
    [TestCase("\\\\server\\share\\root2\\subdir", "\\\\server\\share\\root", false)]
    [TestCase("//server/share/root/subdir", "//server/share/root", true)]
    [TestCase("//server/share/root/subdir", "//server/share/root2", false)]
    [TestCase("//server/share/root2/subdir", "//server/share/root", false)]
    [TestCase("//server/share/root", "//server/share/root", true)]
    [TestCase("//server/share/root2", "//server/share/root", false)]
    [TestCase("//server/share/root", "//server/share/root2", false)]
    [Test]
    public static void TestDoesPathSubsumePath(string full, string check, bool expected)
    {
        PathSegment fullPath = new PathSegment(full);
        PathSegment checkPath = new PathSegment(check);

        Assert.AreEqual(expected, PathSegment.DoesPathSubsumePath(fullPath, checkPath));
    }

    [TestCase("c:/left", "c:/left1", false)]
    [TestCase("c:/left", "c:/Left", true)]
    [TestCase("c:/left", "c:/left", true)]
    [Test]
    public static void TestPathSegmentHashCompare(string left, string right, bool expected)
    {
        PathSegment leftPath = new PathSegment(left);
        PathSegment rightPath = new PathSegment(right);

        Assert.AreEqual(expected, leftPath == rightPath);
    }

    [TestCase("c:/test", new string[] { "c:/test" }, 1)]
    [TestCase("c:/Test", new string[] { "c:/test" }, 1)]
    [TestCase("c:/test", new string[] { "c:/test", "c:/Test" }, 2)]
    [Test]
    public static void TestPathSegmentHashAsKey(string test, string[] map, int expected)
    {
        Dictionary<PathSegment, int> items = new();

        foreach (string mapItem in map)
        {
            PathSegment path = new PathSegment(mapItem);

            items.TryAdd(path, 0);
            items[path]++;
        }

        PathSegment testPath = new PathSegment(test);

        Assert.AreEqual(expected, items[testPath]);
    }
}
