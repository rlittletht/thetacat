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

    [TestCase("foo.txt", new string[]{})]
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
}
