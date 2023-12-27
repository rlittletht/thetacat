using Thetacat.Model;

namespace Tests.Model.MediaItems;

public class TestMediaItemDiff
{
    [TestCase(0, 0, false, false, false, false, false)]
    [TestCase(0, MediaItemDiff.UpdatedValues.MimeType, true, false, false, false, false)]
    [TestCase(0, MediaItemDiff.UpdatedValues.Path, false, true, false, false, false)]
    [TestCase(0, MediaItemDiff.UpdatedValues.MD5, false, false, true, false, false)]
    [TestCase(0, MediaItemDiff.UpdatedValues.State, false, false, false, true, false)]
    [TestCase(0, MediaItemDiff.UpdatedValues.Tags, false, false, false, false, true)]
    [TestCase(MediaItemDiff.UpdatedValues.MimeType, MediaItemDiff.UpdatedValues.MimeType, true, false, false, false, false)]
    [TestCase(MediaItemDiff.UpdatedValues.MimeType, MediaItemDiff.UpdatedValues.Path, true, true, false, false, false)]
    [TestCase(MediaItemDiff.UpdatedValues.MimeType, MediaItemDiff.UpdatedValues.MD5, true, false, true, false, false)]
    [TestCase(MediaItemDiff.UpdatedValues.MimeType, MediaItemDiff.UpdatedValues.State, true, false, false, true, false)]
    [TestCase(MediaItemDiff.UpdatedValues.MimeType, MediaItemDiff.UpdatedValues.Tags, true, false, false, false, true)]
    [Test]
    public static void TestUpdatedValuesFlags(
        MediaItemDiff.UpdatedValues initial,
        MediaItemDiff.UpdatedValues set,
        bool IsMimeTypeChangedExpected,
        bool IsPathChangedExpected,
        bool IsMD5ChangedExpected,
        bool IsStateChangedExpected,
        bool IsTagsChangedExpected)
    {
        MediaItemDiff diff = new MediaItemDiff(Guid.NewGuid());

        diff.PropertiesChanged = initial;
        diff.PropertiesChanged |= set;

        Assert.AreEqual(IsMimeTypeChangedExpected, diff.IsMimeTypeChanged);
        Assert.AreEqual(IsPathChangedExpected, diff.IsPathChanged);
        Assert.AreEqual(IsMD5ChangedExpected, diff.IsMD5Changed);
        Assert.AreEqual(IsStateChangedExpected, diff.IsStateChanged);
        Assert.AreEqual(IsTagsChangedExpected, diff.IsTagsChanged);
    }
}
