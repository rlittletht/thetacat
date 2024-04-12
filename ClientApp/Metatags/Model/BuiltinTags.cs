using System;
using Thetacat.Standards;

namespace Thetacat.Metatags.Model;

public class BuiltinTags
{
    // NOTE: Make sure you update EnsureBuiltinMetatagsDefined if you add a tag here!!

    // these are built-in mediatags. 
    public static readonly Guid s_CatRootID = new Guid("F154F44E-8075-4BAC-A9C2-167D5815C195");
    public static readonly Guid s_WidthID = new Guid("B84B3981-C69C-4C5A-9928-377B8FEC5FBC");
    public static readonly Guid s_HeightID = new Guid("FE34A67F-DD8C-41F8-A914-EE1E5628D292");
    public static readonly Guid s_OriginalMediaDateID = new Guid("BDE93371-2422-489E-86AD-462E2EC975CD");
    public static readonly Guid s_ImportDateID = new Guid("DA8D46B7-10AE-4B69-9EDD-AB4E97F6AB4B");
    public static readonly Guid s_TransformRotateID = new Guid("1A057F3D-B342-4BDE-A6D9-5C529DCAAD4F");
    public static readonly Guid s_TransformMirrorID = new Guid("E50BBD66-D1C9-4D4A-B33D-28A534982883");

    public static readonly Metatag s_Width = Metatag.Create(s_CatRootID, "width", "width", MetatagStandards.Standard.Cat, s_WidthID);
    public static readonly Metatag s_Height = Metatag.Create(s_CatRootID, "height", "height", MetatagStandards.Standard.Cat, s_HeightID);
    public static readonly Metatag s_OriginalMediaDate = Metatag.Create(s_CatRootID, "originalMediaDate", "originalMediaDate", MetatagStandards.Standard.Cat, s_OriginalMediaDateID);
    public static readonly Metatag s_ImportDate = Metatag.Create(s_CatRootID, "importDate", "importDate", MetatagStandards.Standard.Cat, s_ImportDateID);

    // transforms
    public static readonly Metatag s_TransformRotate = Metatag.Create(s_CatRootID, "transformRotate", "transformRotate", MetatagStandards.Standard.Cat, s_TransformRotateID);
    public static readonly Metatag s_TransformMirror = Metatag.Create(s_CatRootID, "transformMirror", "transformMirror", MetatagStandards.Standard.Cat, s_TransformMirrorID);
}
