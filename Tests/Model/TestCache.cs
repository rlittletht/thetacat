using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Tests.Model.Sql;
using Tests.Model.Workgroups;
using Thetacat;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Tests.Model;

public class TestCache
{
    private static Guid media1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static Guid media2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static Guid media3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static Guid media4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static Guid media5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static Guid media6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
    private static Guid media7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
    private static Guid media8 = Guid.Parse("00000000-0000-0000-0000-000000000008");

    private static Guid client1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static Guid client2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

    static MediaItem mediaItem1 = new MediaItem(
        new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem2 = new MediaItem(
        new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem3 = new MediaItem(
        new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem4 = new MediaItem(
        new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem5 = new MediaItem(
        new ServiceMediaItem() { Id = media5, VirtualPath = "media5.jpg", MD5 = "md5-5==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem6 = new MediaItem(
        new ServiceMediaItem() { Id = media6, VirtualPath = "media6.jpg", MD5 = "md5-6==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem7 = new MediaItem(
        new ServiceMediaItem() { Id = media7, VirtualPath = "media7.jpg", MD5 = "md5-7==", MimeType = "image/jpeg", State = "pending" });

    static MediaItem mediaItem8 = new MediaItem(
        new ServiceMediaItem() { Id = media8, VirtualPath = "media8.jpg", MD5 = "md5-8==", MimeType = "image/jpeg", State = "pending" });

    private static string Match1Value = "\\('[^']*',[ ]*'[^']*',[ ]*'[^']*',[ ]*null,[ ]*[0-9]+\\)[ ]*";

    // DoForegroundCache is going to:
    //   * Call RefreshWorkgroupMedia
    //        * Sync workgroup media (to know which items are already cached/being cached by other clients)
    //        * Get the workgroup vector clock
    //   * Figure out the next items for the queue (to do this, it needs to have a populated catalog
    //   * Push the changes back to the server

    [Test]
    public static void TestQueueCacheDownloads_Client1_NoCoherencyFailures_AllItemsQueued()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching the items 2 and 3,
        // which leaves us media items 1 and 4 to cache. 

        AppState state = new AppState();
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, client1);

        cacheMock.SetWorkgroup(workgroupMock);

        state.OverrideCache(cacheMock);
        state.OverrideCatalog(catalogMock);

        MainWindow.SetStateForTests(state);

        // setup test data


        // NOTE THAT (?s) means "single line mode" -- "." matches anything *including* newlines
        sqlSim.SetQuerySources(
            new SqlSimQueryDataItem[]
            {
                // initial workgroup is empty
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*tcat_workgroup_media.*INNER JOIN.*tcat_workgroup_clients").IsMatch(query),
                    new object?[][]
                    {
                    }),
                // with a vectorclock of 1
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 1 }
                    }),
                // and the next vectorclock query will be 1, giving no coherency failure
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 1 }
                    })
            });

        sqlSim.SetNonQueryValidation(
            new[]
            {
                // the first update will insert the media items we pulled to queue
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s)INSERT INTO tcat_workgroup_media").IsMatch(query),
                    (query) =>
                    {
                        // there should be 2 items in this insert
                        Assert.IsTrue(new Regex($"{Match1Value},{Match1Value}").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media1.ToString()}',[ ]*'media1.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media2.ToString()}',[ ]*'media2.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media3.ToString()}',[ ]*'media3.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media4.ToString()}',[ ]*'media4.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));

                        // make sure there aren't 5
                        Assert.IsFalse(new Regex($"{Match1Value},{Match1Value},{Match1Value},{Match1Value},{Match1Value}").IsMatch(query));
                    }),
                // the next item updates the workgroup clock
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s).*UPDATE.*tcat_workgroup_vectorclock.*SET.*value").IsMatch(query),
                    (query) =>
                    {
                        Assert.IsTrue(
                            new Regex("(?s)UPDATE tcat_workgroup_vectorclock SET value = @VectorClock WHERE clock = 'workgroup-clock'").IsMatch(query));
                    },
                    new SqlCommandSim(
                        new Dictionary<string, object>()
                        {
                            { "@VectorClock", 2 }
                        })),
                // and finally the vector clock for this client
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s).*UPDATE.*tcat_workgroup_clients.*SET.*vectorClock").IsMatch(query),
                    (query) =>
                    {
                        Assert.IsTrue(
                            new Regex("(?s)UPDATE.*tcat_workgroup_clients.*SET.*vectorClock.*=.*@VectorClock.*WHERE.*id.*=.*@Id")
                               .IsMatch(query));
                    },
                    new SqlCommandSim(
                        new Dictionary<string, object>()
                        {
                            { "@Id", client1.ToString() },
                            { "@VectorClock", 2 }
                        }))
            });

        cacheMock.QueueCacheDownloads(4);
        
        // there should now be 2 items in our queue
        cacheMock.VerifyQueueContains(new MediaItem[] { mediaItem1, mediaItem2, mediaItem3, mediaItem4 });
    }

    [Test]
    public static void TestQueueCacheDownloads_Client1_SingleCoherencyFailure_PartialOverlap()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching the items 2 and 3,
        // which leaves us media items 1 and 4 to cache. 

        AppState state = new AppState();
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, client1);

        cacheMock.SetWorkgroup(workgroupMock);

        state.OverrideCache(cacheMock);
        state.OverrideCatalog(catalogMock);

        MainWindow.SetStateForTests(state);

        // setup test data


        // NOTE THAT (?s) means "single line mode" -- "." matches anything *including* newlines
        sqlSim.SetQuerySources(
            new SqlSimQueryDataItem[]
            {
                // initial workgroup is empty
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*tcat_workgroup_media.*INNER JOIN.*tcat_workgroup_clients").IsMatch(query),
                    new object?[][]
                    {
                    }),
                // with a vectorclock of 1
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 1 }
                    }),
                // and the next vectorclock query will be 2, giving a coherency failure
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 2 }
                    }),
                // so now 3WM will fetch everything again
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 2 }
                    }),
                //  and the next workgroup refresh gives media2 and media3 claimed by client2
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*tcat_workgroup_media.*INNER JOIN.*tcat_workgroup_clients").IsMatch(query),
                    new[]
                    {
                        new object?[] { media2.ToString(), "path2", client2.ToString(), null, 1 },
                        new object?[] { media3.ToString(), "path3", client2.ToString(), null, 1 }
                    }),
                // after 3WM we will try to upload again, which will again fech the vector clock. let it succeed with
                // the same VC
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 2 }
                    }),
            });

        sqlSim.SetNonQueryValidation(
            new[]
            {
                // the first update will insert the media items we pulled to queue
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s)INSERT INTO tcat_workgroup_media").IsMatch(query),
                    (query) =>
                    {
                        // there should be 2 items in this insert
                        Assert.IsTrue(new Regex($"{Match1Value},{Match1Value}").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media1.ToString()}',[ ]*'media1.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*3\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{media4.ToString()}',[ ]*'media4.jpg',[ ]*'{client1.ToString()}',[ ]*null,[ ]*3\\)").IsMatch(query));

                        // make sure there aren't 3
                        Assert.IsFalse(new Regex($"{Match1Value},{Match1Value},{Match1Value}").IsMatch(query));
                    }),
                // the next item updates the workgroup clock
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s).*UPDATE.*tcat_workgroup_vectorclock.*SET.*value").IsMatch(query),
                    (query) =>
                    {
                        Assert.IsTrue(
                            new Regex("(?s)UPDATE tcat_workgroup_vectorclock SET value = @VectorClock WHERE clock = 'workgroup-clock'").IsMatch(query));
                    },
                    new SqlCommandSim(
                        new Dictionary<string, object>()
                        {
                            { "@VectorClock", 3 }
                        })),
                // and finally the vector clock for this client
                new SqlSimNonQueryDataItem(
                    (query) => new Regex("(?s).*UPDATE.*tcat_workgroup_clients.*SET.*vectorClock").IsMatch(query),
                    (query) =>
                    {
                        Assert.IsTrue(
                            new Regex("(?s)UPDATE.*tcat_workgroup_clients.*SET.*vectorClock.*=.*@VectorClock.*WHERE.*id.*=.*@Id")
                               .IsMatch(query));
                    },
                    new SqlCommandSim(
                        new Dictionary<string, object>()
                        {
                            { "@Id", client1.ToString() },
                            { "@VectorClock", 3 }
                        }))
            });

        cacheMock.QueueCacheDownloads(4);

        // there should now be 2 items in our queue
        cacheMock.VerifyQueueContains(new MediaItem[] { mediaItem1, mediaItem4 });
    }

    [Test]
    public static void TestQueueCacheDownloads_Client1_SingleCoherencyFailure_FulllOverlap()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching all 4 itmes,
        // which leaves us with nothing to update

        AppState state = new AppState();
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, client1);

        cacheMock.SetWorkgroup(workgroupMock);

        state.OverrideCache(cacheMock);
        state.OverrideCatalog(catalogMock);

        MainWindow.SetStateForTests(state);

        // setup test data


        // NOTE THAT (?s) means "single line mode" -- "." matches anything *including* newlines
        sqlSim.SetQuerySources(
            new SqlSimQueryDataItem[]
            {
                // initial workgroup is empty
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*tcat_workgroup_media.*INNER JOIN.*tcat_workgroup_clients").IsMatch(query),
                    new object?[][]
                    {
                    }),
                // with a vectorclock of 1
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 1 }
                    }),
                // and the next vectorclock query will be 2, giving a coherency failure
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 2 }
                    }),
                // so now 3WM will fetch everything again
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*vectorclock.*workgroup-clock").IsMatch(query),
                    new[]
                    {
                        new object?[] { 2 }
                    }),
                //  and the next workgroup refresh gives media2 and media3 claimed by client2
                new(
                    (query) => new Regex("(?s)SELECT.*FROM.*tcat_workgroup_media.*INNER JOIN.*tcat_workgroup_clients").IsMatch(query),
                    new[]
                    {
                        new object?[] { media1.ToString(), "path1", client2.ToString(), null, 1 },
                        new object?[] { media2.ToString(), "path2", client2.ToString(), null, 1 },
                        new object?[] { media3.ToString(), "path3", client2.ToString(), null, 1 },
                        new object?[] { media4.ToString(), "path4", client2.ToString(), null, 1 }
                    }),
                // after 3WM we will have nothing to update, so we are done
            });

        sqlSim.SetNonQueryValidation(
            new SqlSimNonQueryDataItem[]
            {
                // there are no updates
            });

        cacheMock.QueueCacheDownloads(4);

        // our queue should be empty
        cacheMock.VerifyQueueContains(new MediaItem[] { });
    }

}
