using System;
using Thetacat.Standards;

namespace Thetacat.Metatags.Model;

public class BuiltinTags_Current
{
    // NOTE: Make sure you update EnsureBuiltinMetatagsDefined if you add a tag here!!

    // these are built-in mediatags. 
    public static readonly Guid s_UserRootID = new Guid("420a5648-c598-4513-9b14-019529edf584");
    public static readonly Guid s_CatRootID = new Guid("91fc26df-861b-42b5-903b-019529edf592");
    public static readonly Guid s_WidthID = new Guid("1cb197ff-7e04-4f2e-b88a-019529f4a985");
    public static readonly Guid s_HeightID = new Guid("5072b5f5-df9b-4a0a-a199-019529f4a993");
    public static readonly Guid s_OriginalMediaDateID = new Guid("6b27f33d-f065-4ef7-95d3-019529f4e891");
    public static readonly Guid s_DateSpecifiedID = new Guid("cc1c2651-1c89-4868-a4e3-019529f4e89f");
    public static readonly Guid s_ImportDateID = new Guid("17156a81-5d52-42e0-90bb-019529f52eca");
    public static readonly Guid s_TransformRotateID = new Guid("d06d038b-5718-4f10-8b31-019529f52ed8");
    public static readonly Guid s_TransformMirrorID = new Guid("ae20f1ea-c765-457d-83fa-019529f56ce4");
    public static readonly Guid s_IsTrashItemID = new Guid("14d65c31-d7e3-4dd1-ad74-019529f56cf2");
    public static readonly Guid s_DontPushToCloudID = new Guid("0d7249fb-7fc2-4def-bc5e-019529f5ae99");

    // VirtualPath isn't a builtin tag that can be set in the database -- its only for filtering
    // (thus s_BuiltinTags doesn't include it)
    public static readonly Guid s_VirtualPathID = new Guid("7EE164AF-57A7-4B86-8E76-7143CA0D176E");

    public static readonly Metatag s_Width = Metatag.Create(s_CatRootID, "width", "width", MetatagStandards.Standard.Cat, s_WidthID);
    public static readonly Metatag s_Height = Metatag.Create(s_CatRootID, "height", "height", MetatagStandards.Standard.Cat, s_HeightID);
    public static readonly Metatag s_OriginalMediaDate = Metatag.Create(s_CatRootID, "originalMediaDate", "originalMediaDate", MetatagStandards.Standard.Cat, s_OriginalMediaDateID);
    public static readonly Metatag s_ImportDate = Metatag.Create(s_CatRootID, "importDate", "importDate", MetatagStandards.Standard.Cat, s_ImportDateID);
    public static readonly Metatag s_DateSpecified = Metatag.Create(s_CatRootID, "specifiedDate", "Manually specified date", MetatagStandards.Standard.Cat, s_DateSpecifiedID);
    // transforms
    public static readonly Metatag s_TransformRotate = Metatag.Create(s_CatRootID, "transformRotate", "transformRotate", MetatagStandards.Standard.Cat, s_TransformRotateID);
    public static readonly Metatag s_TransformMirror = Metatag.Create(s_CatRootID, "transformMirror", "transformMirror", MetatagStandards.Standard.Cat, s_TransformMirrorID);

    // designate an item as "trashed" -- pending removal from the catalog
    public static readonly Metatag s_IsTrashItem = Metatag.Create(s_CatRootID, "isTrashItem", "item pending deletion", MetatagStandards.Standard.Cat, s_IsTrashItemID);

    // this item shouldn't be pushed to the cloud -- should only live in the local workgroup (other workgroups will see this is
    // forever "cache pending")
    public static readonly Metatag s_DontPushToCloud = Metatag.Create(s_CatRootID, "dontPushToCloud", "local workgroup only", MetatagStandards.Standard.Cat, s_DontPushToCloudID);

    // this is just for querying virtual path
    public static readonly Metatag s_VirtualPath = Metatag.Create(s_CatRootID, "virtualPath", "virtual path", MetatagStandards.Standard.Cat, s_VirtualPathID);

    static BuiltinTags_Current(){}

    public static void Initialize()
    {}
}
