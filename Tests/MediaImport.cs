namespace Tests;

public class MediaImport
{
    [TestCase("c:/", "c:/test", "foo.jpg", false, true, null, null, "test/foo.jpg")]
    [TestCase("c:/", "c:/test/", "foo.jpg", false, true, null, null, "test/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", false, true, null, null, "test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", false, true, null, "prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", false, true, null, "prefix/", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", false, true, null, "/prefix/", "prefix/test/bar/foo.jpg")]
    [TestCase("c:/", "c:/test/bar", "foo.jpg", false, true, null, "/prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, true, null, null, "test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, true, "/imports", null, "imports/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, true, "/imports", "prefix", "imports/prefix/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, true, "imports", "prefix", "imports/prefix/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, true, "/imports", "/prefix", "imports/prefix/test/bar/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, false, null, null, "foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, false, "/imports", null, "imports/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, false, "/imports", "prefix", "imports/prefix/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, false, "imports", "prefix", "imports/prefix/foo.jpg")]
    [TestCase("//pix/pix", "//pix/pix/test/bar", "foo.jpg", false, false, "/imports", "/prefix", "imports/prefix/foo.jpg")]
    [TestCase("c:\\", "c:\\test", "foo.jpg", false, true, null, null, "test/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\", "foo.jpg", false, true, null, null, "test/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, true, null, null, "test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, true, null, "prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, true, null, "prefix\\", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, true, null, "\\prefix\\", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, true, null, "\\prefix", "prefix/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, false, null, "\\prefix", "prefix/foo.jpg")]
    [TestCase("c:\\", "c:\\test\\bar", "foo.jpg", false, false, null, null, "foo.jpg")]

    [TestCase("c:/", "c:/test", "foo.jpg", true, false, null, null, "foo.jpg")]
    [TestCase("c:/parent", "c:/parent/test/", "foo.jpg", true, false, null, null, "parent/foo.jpg")]
    [TestCase("c:/grandparent/parent/", "c:/grandparent/parent/test/", "foo.jpg", true, false, null, null, "parent/foo.jpg")]
    [TestCase("//pix/pix/parent", "//pix/pix/parent/test/bar", "foo.jpg", true, false, null, null, "parent/foo.jpg")]
    [TestCase("c:\\", "c:/test", "foo.jpg", true, false, null, null, "foo.jpg")]
    [TestCase("c:\\parent", "c:\\parent\\test\\", "foo.jpg", true, false, null, null, "parent/foo.jpg")]
    [TestCase("c:\\grandparent\\parent\\", "c:/grandparent/parent/test/", "foo.jpg", true, false, null, null, "parent/foo.jpg")]
    [TestCase("//pix/pix/parent", "//pix/pix/parent/test/bar", "foo.jpg", true, false, null, null, "parent/foo.jpg")]

    [TestCase("c:/", "c:/test", "foo.jpg", true, true, null, null, "test/foo.jpg")]
    [TestCase("c:/parent", "c:/parent/test/", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("c:/grandparent/parent", "c:/grandparent/parent/test/", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("c:/grandparent/parent/", "c:/grandparent/parent/test/", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("//pix/pix/parent", "//pix/pix/parent/test/bar", "foo.jpg", true, true, null, null, "parent/test/bar/foo.jpg")]
    [TestCase("c:\\", "c:/test", "foo.jpg", true, true, null, null, "test/foo.jpg")]
    [TestCase("c:\\parent", "c:\\parent\\test\\", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("c:\\grandparent\\parent", "c:/grandparent/parent/test/", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("c:\\grandparent\\parent\\", "c:/grandparent/parent/test/", "foo.jpg", true, true, null, null, "parent/test/foo.jpg")]
    [TestCase("//pix/pix/parent", "//pix/pix/parent/test/bar", "foo.jpg", true, true, null, null, "parent/test/bar/foo.jpg")]

    [Test]
    public static void TestBuildVirtualPath(string sourcePath, string itemPath, string itemName, bool includeParentDir, bool includeSubdirs, string? virtualRoot, string? virtualPrefix, string expected)
    {
        string actual = Thetacat.Import.UI.MediaImport.BuildVirtualPath(sourcePath, itemPath, itemName, includeParentDir, includeSubdirs, virtualRoot, virtualPrefix);

        Assert.AreEqual(expected, actual);
    }
}
