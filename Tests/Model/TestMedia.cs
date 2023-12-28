using Thetacat.Model;
using Thetacat.ServiceClient;

namespace Tests.Model;

public class TestMedia
{
    public static Guid media1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid media2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static Guid media3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static Guid media4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static Guid media5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
    public static Guid media6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
    public static Guid media7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
    public static Guid media8 = Guid.Parse("00000000-0000-0000-0000-000000000008");

    public static Guid client1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static Guid client2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public static MediaItem mediaItem1 = new MediaItem(
        new ServiceMediaItem() { Id = media1, VirtualPath = "media1.jpg", MD5 = "md5-1==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem2 = new MediaItem(
        new ServiceMediaItem() { Id = media2, VirtualPath = "media2.jpg", MD5 = "md5-2==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem3 = new MediaItem(
        new ServiceMediaItem() { Id = media3, VirtualPath = "media3.jpg", MD5 = "md5-3==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem4 = new MediaItem(
        new ServiceMediaItem() { Id = media4, VirtualPath = "media4.jpg", MD5 = "md5-4==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem5 = new MediaItem(
        new ServiceMediaItem() { Id = media5, VirtualPath = "media5.jpg", MD5 = "md5-5==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem6 = new MediaItem(
        new ServiceMediaItem() { Id = media6, VirtualPath = "media6.jpg", MD5 = "md5-6==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem7 = new MediaItem(
        new ServiceMediaItem() { Id = media7, VirtualPath = "media7.jpg", MD5 = "md5-7==", MimeType = "image/jpeg", State = "pending" });

    public static MediaItem mediaItem8 = new MediaItem(
        new ServiceMediaItem() { Id = media8, VirtualPath = "media8.jpg", MD5 = "md5-8==", MimeType = "image/jpeg", State = "pending" });
}
