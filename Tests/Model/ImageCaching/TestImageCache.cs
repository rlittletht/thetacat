using System.Windows.Media.Imaging;
using Thetacat;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;

namespace Tests.Model.ImageCaching;

public class TestImageCache
{
    [Test]
    public static void TestImageCacheUpdate_NoEventHandler()
    {
        MainWindow.InUnitTest = true;
        ImageCache cache = new ImageCache();
        
        cache.TryQueueBackgroundLoadToCache(TestMedia.mediaItem1, "");

        cache.ResetImageForKey(TestMedia.mediaItem1.ID);
        // if we get here, we passed (just looking for a crash...)
        Assert.IsTrue(true);
    }

    [Test]
    public static void TestImageCacheUpdate_OneEventHandler_OneItem()
    {
        MainWindow.InUnitTest = true;
        ImageCache cache = new ImageCache();

        cache.TryQueueBackgroundLoadToCache(TestMedia.mediaItem1, "");

        int counter = 0;

        cache.ImageCacheUpdated += new EventHandler<ImageCacheUpdateEventArgs>(
            (sender, args) =>
            {
                Assert.AreEqual(TestMedia.mediaItem1.ID, args.MediaId);
                counter += 1;
            });

        Assert.AreEqual(0, counter);
        cache.ForceImageNotNullForTest(TestMedia.mediaItem1.ID);
        cache.ResetImageForKey(TestMedia.mediaItem1.ID);
        Assert.AreEqual(1, counter);
    }

    [Test]
    public static void TestImageCacheUpdate_TwoEventHandlers_OneItem()
    {
        MainWindow.InUnitTest = true;
        ImageCache cache = new ImageCache();

        cache.TryQueueBackgroundLoadToCache(TestMedia.mediaItem1, "");

        int counter = 0;

        cache.ImageCacheUpdated += new EventHandler<ImageCacheUpdateEventArgs>(
            (sender, args) =>
            {
                Assert.AreEqual(TestMedia.mediaItem1.ID, args.MediaId);
                counter += 1;
            });

        cache.ImageCacheUpdated += new EventHandler<ImageCacheUpdateEventArgs>(
            (sender, args) =>
            {
                Assert.AreEqual(TestMedia.mediaItem1.ID, args.MediaId);
                counter += 10;
            });

        Assert.AreEqual(0, counter);
        cache.ForceImageNotNullForTest(TestMedia.mediaItem1.ID);
        cache.ResetImageForKey(TestMedia.mediaItem1.ID);
        Assert.AreEqual(11, counter);
    }

    [Test]
    public static void TestImageCacheUpdate_OneEventHandler_AfterRemove_OneItem()
    {
        MainWindow.InUnitTest = true;
        ImageCache cache = new ImageCache();

        cache.TryQueueBackgroundLoadToCache(TestMedia.mediaItem1, "");

        int counter = 0;

        EventHandler<ImageCacheUpdateEventArgs> testHandler = new(
            (sender, args) =>
            {
                Assert.AreEqual(TestMedia.mediaItem1.ID, args.MediaId);
                counter += 1;
            });

        cache.ImageCacheUpdated += testHandler;

        cache.ImageCacheUpdated += new EventHandler<ImageCacheUpdateEventArgs>(
            (sender, args) =>
            {
                Assert.AreEqual(TestMedia.mediaItem1.ID, args.MediaId);
                counter += 10;
            });

        cache.ImageCacheUpdated -= testHandler;

        Assert.AreEqual(0, counter);
        cache.ForceImageNotNullForTest(TestMedia.mediaItem1.ID);
        cache.ResetImageForKey(TestMedia.mediaItem1.ID);
        Assert.AreEqual(10, counter);
    }
}
