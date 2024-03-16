using System.Text.RegularExpressions;
using Tests.Model.Sql;
using Tests.Model.Workgroups;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Tests.Model;

public class TestCache
{
    private static string Match1Value = "\\('[^']*',[ ]*'[^']*',[ ]*'[^']*',[ ]*null,[ ]*[0-9]+\\)[ ]*";

    private static void CloseMonitorMock(bool skip)
    {
    }

    private static void AddWorkMock(string description, BackgroundWorkerWork<bool> work)
    {

    }

// StartBackgroundCaching is going to:
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
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "active" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, TestMedia.client1);

        cacheMock.SetWorkgroup(workgroupMock);

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
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media1.ToString()} *',[ ]*'media1.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media2.ToString()} *',[ ]*'media2.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media3.ToString()} *',[ ]*'media3.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media4.ToString()} *',[ ]*'media4.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));

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
                            { "@Id", TestMedia.client1.ToString() },
                            { "@VectorClock", 2 }
                        }))
            });

        cacheMock.QueueCacheDownloadsFromMedia(catalogMock.GetMediaCollection(), cacheMock, 4);
        
        // there should now be 2 items in our queue
        cacheMock.VerifyQueueContains(new MediaItem[] { TestMedia.mediaItem1, TestMedia.mediaItem2, TestMedia.mediaItem3, TestMedia.mediaItem4 });
    }

    [Test]
    public static void TestQueueCacheDownloads_Client1_NoCoherencyFailures_NonPendingItemsQueued()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching the items 2 and 3,
        // which leaves us media items 1 and 4 to cache. 
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "active" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, TestMedia.client1);

        cacheMock.SetWorkgroup(workgroupMock);

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
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media1.ToString()} *',[ ]*'media1.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media4.ToString()} *',[ ]*'media4.jpg',[ ]*' *{TestMedia.client1.ToString()}',[ ]*null,[ ]*2\\)").IsMatch(query));

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
                            { "@Id", TestMedia.client1.ToString() },
                            { "@VectorClock", 2 }
                        }))
            });

        cacheMock.QueueCacheDownloadsFromMedia(catalogMock.GetMediaCollection(), cacheMock, 4);

        // there should now be 2 items in our queue
        cacheMock.VerifyQueueContains(new MediaItem[] { TestMedia.mediaItem1, TestMedia.mediaItem4 });
    }

    [Test]
    public static void TestQueueCacheDownloads_Client1_SingleCoherencyFailure_PartialOverlap()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching the items 2 and 3,
        // which leaves us media items 1 and 4 to cache. 
        
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "active" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, TestMedia.client1);

        cacheMock.SetWorkgroup(workgroupMock);

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
                        new object?[] { TestMedia.media2.ToString(), "path2", TestMedia.client2.ToString(), null, 1 },
                        new object?[] { TestMedia.media3.ToString(), "path3", TestMedia.client2.ToString(), null, 1 }
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
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media1.ToString()}',[ ]*'media1.jpg',[ ]*'{TestMedia.client1.ToString()}',[ ]*null,[ ]*3\\)").IsMatch(query));
                        Assert.IsTrue(new Regex($"\\('{TestMedia.media4.ToString()}',[ ]*'media4.jpg',[ ]*'{TestMedia.client1.ToString()}',[ ]*null,[ ]*3\\)").IsMatch(query));

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
                            { "@Id", TestMedia.client1.ToString() },
                            { "@VectorClock", 3 }
                        }))
            });

        cacheMock.QueueCacheDownloadsFromMedia(catalogMock.GetMediaCollection(), cacheMock, 4);

        // there should now be 2 items in our queue
        cacheMock.VerifyQueueContains(new MediaItem[] { TestMedia.mediaItem1, TestMedia.mediaItem4 });
    }

    [Test]
    public static void TestQueueCacheDownloads_Client1_SingleCoherencyFailure_FulllOverlap()
    {
        // in this test, the media database has 4 items. initially this client get's all 4 items to queue, but 
        // gets a coherency failure trying to update the WG DB. The DB has client2 caching all 4 itmes,
        // which leaves us with nothing to update
        CacheMock cacheMock = new CacheMock();

        List<MediaItem> MediaItems_1_4 =
            new()
            {
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "active" }),
                new MediaItem(new ServiceMediaItem() { Id = TestMedia.media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "active" }),
            };

        CatalogMock catalogMock = new CatalogMock(MediaItems_1_4);
        SqlSim sqlSim = new SqlSim();

        WorkgroupMock workgroupMock = new WorkgroupMock(sqlSim, TestMedia.client1);

        cacheMock.SetWorkgroup(workgroupMock);

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
                        new object?[] { TestMedia.media1.ToString(), "path1", TestMedia.client2.ToString(), null, 1 },
                        new object?[] { TestMedia.media2.ToString(), "path2", TestMedia.client2.ToString(), null, 1 },
                        new object?[] { TestMedia.media3.ToString(), "path3", TestMedia.client2.ToString(), null, 1 },
                        new object?[] { TestMedia.media4.ToString(), "path4", TestMedia.client2.ToString(), null, 1 }
                    }),
                // after 3WM we will have nothing to update, so we are done
            });

        sqlSim.SetNonQueryValidation(
            new SqlSimNonQueryDataItem[]
            {
                // there are no updates
            });

        cacheMock.QueueCacheDownloadsFromMedia(catalogMock.GetMediaCollection(), cacheMock, 4);

        // our queue should be empty
        cacheMock.VerifyQueueContains(new MediaItem[] { });
    }

}
