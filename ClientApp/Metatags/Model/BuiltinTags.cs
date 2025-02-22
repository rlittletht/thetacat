using System;
using Thetacat.Standards;

namespace Thetacat.Metatags.Model;

/*----------------------------------------------------------------------------
    %%Class: BuiltinTags

    stored on the schema, when instantiated it will choose either current
    or deprecated tags
----------------------------------------------------------------------------*/
public class BuiltinTags
{
    // NOTE: Make sure you update EnsureBuiltinMetatagsDefined if you add a tag here!!
    // ALSO make sure you update to make sure you update MapDeprecatedIdToCurrentId
    // to map the old id to the new id

    // these are built-in mediatags. 
    public readonly Guid UserRootID;
    public readonly Guid CatRootID;
    public readonly Guid WidthID;
    public readonly Guid HeightID;
    public readonly Guid OriginalMediaDateID;
    public readonly Guid DateSpecifiedID;
    public readonly Guid ImportDateID;
    public readonly Guid TransformRotateID;
    public readonly Guid TransformMirrorID;
    public readonly Guid IsTrashItemID;
    public readonly Guid DontPushToCloudID;

    // VirtualPath isn't a builtin tag that can be set in the database -- its only for filtering
    // (thus s_BuiltinTags doesn't include it)
    // (there is no deprecated version of this since it doens't need to be sorted in an SQL index)
    public readonly Guid VirtualPathID = new Guid("7EE164AF-57A7-4B86-8E76-7143CA0D176E");

    public readonly Metatag CatRoot;
    public readonly Metatag UserRoot;
    public readonly Metatag Width;
    public readonly Metatag Height;
    public readonly Metatag OriginalMediaDate;
    public readonly Metatag ImportDate;

    public readonly Metatag DateSpecified;

    // transforms
    public readonly Metatag TransformRotate;
    public readonly Metatag TransformMirror;

    // designate an item as "trashed" -- pending removal from the catalog
    public readonly Metatag IsTrashItem;

    // this item shouldn't be pushed to the cloud -- should only live in the local workgroup (other workgroups will see this is
    // forever "cache pending")
    public readonly Metatag DontPushToCloud;

    // this is just for querying virtual path
    public readonly Metatag VirtualPath;

    public readonly bool UseDeprecatedBuiltinTags;

    public BuiltinTags(bool useDeprecatedBuiltinTags)
    {
        UseDeprecatedBuiltinTags = useDeprecatedBuiltinTags;

        if (!useDeprecatedBuiltinTags)
        {
            UserRootID = BuiltinTags_Current.s_UserRootID;
            CatRootID = BuiltinTags_Current.s_CatRootID;
            WidthID = BuiltinTags_Current.s_WidthID;
            HeightID = BuiltinTags_Current.s_HeightID;
            OriginalMediaDateID = BuiltinTags_Current.s_OriginalMediaDateID;
            DateSpecifiedID = BuiltinTags_Current.s_DateSpecifiedID;
            ImportDateID = BuiltinTags_Current.s_ImportDateID;
            TransformRotateID = BuiltinTags_Current.s_TransformRotateID;
            TransformMirrorID = BuiltinTags_Current.s_TransformMirrorID;
            IsTrashItemID = BuiltinTags_Current.s_IsTrashItemID;
            DontPushToCloudID = BuiltinTags_Current.s_DontPushToCloudID;
        }
        else
        {
            UserRootID = BuiltinTags_Deprecated.s_UserRootID;
            CatRootID = BuiltinTags_Deprecated.s_CatRootID;
            WidthID = BuiltinTags_Deprecated.s_WidthID;
            HeightID = BuiltinTags_Deprecated.s_HeightID;
            OriginalMediaDateID = BuiltinTags_Deprecated.s_OriginalMediaDateID;
            DateSpecifiedID = BuiltinTags_Deprecated.s_DateSpecifiedID;
            ImportDateID = BuiltinTags_Deprecated.s_ImportDateID;
            TransformRotateID = BuiltinTags_Deprecated.s_TransformRotateID;
            TransformMirrorID = BuiltinTags_Deprecated.s_TransformMirrorID;
            IsTrashItemID = BuiltinTags_Deprecated.s_IsTrashItemID;
            DontPushToCloudID = BuiltinTags_Deprecated.s_DontPushToCloudID;
        }

        CatRoot = Metatag.Create(null, "CAT", "cat root", MetatagStandards.Standard.Cat, CatRootID);
        UserRoot = Metatag.Create(null, "user", "user root", MetatagStandards.Standard.User, UserRootID);
        Width = Metatag.Create(CatRootID, "width", "width", MetatagStandards.Standard.Cat, WidthID);
        Height = Metatag.Create(CatRootID, "height", "height", MetatagStandards.Standard.Cat, HeightID);
        OriginalMediaDate = Metatag.Create(CatRootID, "originalMediaDate", "originalMediaDate", MetatagStandards.Standard.Cat, OriginalMediaDateID);
        ImportDate = Metatag.Create(CatRootID, "importDate", "importDate", MetatagStandards.Standard.Cat, ImportDateID);
        DateSpecified = Metatag.Create(CatRootID, "specifiedDate", "Manually specified date", MetatagStandards.Standard.Cat, DateSpecifiedID);
        TransformRotate = Metatag.Create(CatRootID, "transformRotate", "transformRotate", MetatagStandards.Standard.Cat, TransformRotateID);
        TransformMirror = Metatag.Create(CatRootID, "transformMirror", "transformMirror", MetatagStandards.Standard.Cat, TransformMirrorID);
        IsTrashItem = Metatag.Create(CatRootID, "isTrashItem", "item pending deletion", MetatagStandards.Standard.Cat, IsTrashItemID);
        DontPushToCloud = Metatag.Create(CatRootID, "dontPushToCloud", "local workgroup only", MetatagStandards.Standard.Cat, DontPushToCloudID);
        VirtualPath = Metatag.Create(CatRootID, "virtualPath", "virtual path", MetatagStandards.Standard.Cat, VirtualPathID);

        Tags =
        [
            CatRoot,
            UserRoot,
            Width,
            Height,
            OriginalMediaDate,
            ImportDate,
            DateSpecified,
            TransformRotate,
            TransformMirror,
            IsTrashItem,
            DontPushToCloud
        ];

        NonSchemaTags =
        [
            VirtualPath
        ];
    }

    public readonly Metatag[] Tags;
    public readonly Metatag[] NonSchemaTags;

    public static Guid? MapDeprecatedIdToCurrentId(Guid id)
    {
        if (id == BuiltinTags_Deprecated.s_UserRootID || id == BuiltinTags_Current.s_UserRootID) return BuiltinTags_Current.s_UserRootID;
        if (id == BuiltinTags_Deprecated.s_CatRootID || id == BuiltinTags_Current.s_CatRootID) return BuiltinTags_Current.s_CatRootID;
        if (id == BuiltinTags_Deprecated.s_WidthID || id == BuiltinTags_Current.s_WidthID) return BuiltinTags_Current.s_WidthID;
        if (id == BuiltinTags_Deprecated.s_HeightID || id == BuiltinTags_Current.s_HeightID) return BuiltinTags_Current.s_HeightID;
        if (id == BuiltinTags_Deprecated.s_OriginalMediaDateID || id == BuiltinTags_Current.s_OriginalMediaDateID) return BuiltinTags_Current.s_OriginalMediaDateID;
        if (id == BuiltinTags_Deprecated.s_DateSpecifiedID || id == BuiltinTags_Current.s_DateSpecifiedID) return BuiltinTags_Current.s_DateSpecifiedID;
        if (id == BuiltinTags_Deprecated.s_ImportDateID || id == BuiltinTags_Current.s_ImportDateID) return BuiltinTags_Current.s_ImportDateID;
        if (id == BuiltinTags_Deprecated.s_TransformRotateID || id == BuiltinTags_Current.s_TransformRotateID) return BuiltinTags_Current.s_TransformRotateID;
        if (id == BuiltinTags_Deprecated.s_TransformMirrorID || id == BuiltinTags_Current.s_TransformMirrorID) return BuiltinTags_Current.s_TransformMirrorID;
        if (id == BuiltinTags_Deprecated.s_IsTrashItemID || id == BuiltinTags_Current.s_IsTrashItemID) return BuiltinTags_Current.s_IsTrashItemID;
        if (id == BuiltinTags_Deprecated.s_DontPushToCloudID || id == BuiltinTags_Current.s_DontPushToCloudID) return BuiltinTags_Current.s_DontPushToCloudID;

        return null;
    }
}
