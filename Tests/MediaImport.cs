namespace Tests;

public class MediaImport
{
    [TestCase("c:/", "c:/test", "foo.jpg", true, null, "test/foo.jpg")]
    [TestCase("c:/", "c:/test/", "foo.jpg", true, null, "test/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", true, null, "test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", true, "prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", true, "prefix/", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", true, "/prefix/", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", true, "/prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", true, null, "test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test", "foo.jpg", true, null, "test/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\", "foo.jpg", true, null, "test/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", true, null, "test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", true, "prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", true, "prefix\\", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", true, "\\prefix\\", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", true, "\\prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, "\\prefix", "prefix/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, null, "")]
    [Test]
    public static void TestBuildVirtualPath(string sourcePath, string itemPath, string itemName, bool includeSubdirs, string? virtualPrefix, string expected)
    {
        string actual = Thetacat.Import.UI.MediaImport.BuildVirtualPath(sourcePath, itemPath, itemName, includeSubdirs, virtualPrefix);

        Assert.AreEqual(expected, actual);
    }
}
