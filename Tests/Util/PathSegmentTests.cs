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
        PathSegment expected = PathSegment.CreateForTest(expectedSegment, expectedLocal);
        PathSegment actual = PathSegment.CreateFromString(path);

        Assert.AreEqual(expected, actual);
    }

    [TestCase("path1", "path2", "path1/path2", null)]
    [TestCase("path1\\2", "path2", "path1/2/path2", null)]
    [TestCase("path1", "path2\\2", "path1/path2/2", null)]
    [TestCase("path1/", "path2", "path1/path2", null)]
    [TestCase("\\path1", "path2", "/path1/path2", null)]
    [TestCase("\\path1", "\\path2", "/path2", null)]
    [TestCase("/path1", "/path2", "/path2", null)]
    [TestCase("", "/path2", "/path2", null)]
    [TestCase("path1", "", "path1", null)]
    [Test]
    public static void TestPathCombineBoth(string pathA, string pathB, string expectedSegment, string? expectedLocal)
    {
        PathSegment expected = PathSegment.CreateForTest(expectedSegment, expectedLocal);

        PathSegment segmentA = new PathSegment(pathA);
        PathSegment segmentB = new PathSegment(pathB);

        PathSegment actual = PathSegment.Combine(segmentA, segmentB);

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
        PathSegment expected = PathSegment.CreateForTest(expectedSegment, expectedLocal);

        PathSegment actual1 = PathSegment.GetPathRoot(path);
        PathSegment actual2 = PathSegment.GetPathRoot(new PathSegment(path));

        Assert.AreEqual(expected, actual1);
        Assert.AreEqual(expected, actual2);
    }
}
