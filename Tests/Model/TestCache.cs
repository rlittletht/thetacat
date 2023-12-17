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

//    public static ServiceWorkgroupMediaClock Client1CachingItems3_4()
//    {
//        ServiceWorkgroupMediaClock mediaClock =
//            new ServiceWorkgroupMediaClock()
//            {
//                VectorClock = 1,
//                Media =
//                    new List<ServiceWorkgroupItem>()
//                    {
//                        new()
//                        {
//                            MediaId = media3,
//                            CachedBy = client1,
//                            CachedDate = null,
//                            Path = "media3",
//                            VectorClock = 1
//                        }
//                    }
//            }
//    }

    private static List<MediaItem> MediaItems =
        new()
        {
            new MediaItem(new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media5, VirtualPath = "media5.jpg", MD5 = "md5-5==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media6, VirtualPath = "media6.jpg", MD5 = "md5-6==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media7, VirtualPath = "media7.jpg", MD5 = "md5-7==", MimeType = "image/jpeg", State = "pending" }),
            new MediaItem(new ServiceMediaItem() { Id = media8, VirtualPath = "media8.jpg", MD5 = "md5-8==", MimeType = "image/jpeg", State = "pending" })
        };

    public static Dictionary<Guid, MediaItem> Client1ItemsForQueue =
        new()
        {
            { MediaItems[0].ID, MediaItems[0] },
            { MediaItems[1].ID, MediaItems[1] },
            { MediaItems[2].ID, MediaItems[2] },
            { MediaItems[3].ID, MediaItems[3] }
        };

    public static Dictionary<Guid, MediaItem> Client2ItemsForQueue =
        new()
        {
            { MediaItems[2].ID, MediaItems[2] },
            { MediaItems[3].ID, MediaItems[3] },
            { MediaItems[4].ID, MediaItems[4] },
            { MediaItems[5].ID, MediaItems[5] }
        };

    public static void TestDoForegroundCache()
    {
        AppState state = new AppState();
        CacheMock cacheMock = new CacheMock();
        WorkgroupMock workgroupMock = new WorkgroupMock();

        cacheMock.SetWorkgroup(workgroupMock);

        state.OverrideCache(cacheMock);
        MainWindow.SetStateForTests(state);

        // setup test data

  //      workgroupMock.SetMediaClockSource(
  //          () =>
  //          {
  //
  //          });
  //      cacheMock.DoForegroundCache();

        // inspect the queue

    }
}
