using System;
using Thetacat.Standards;

namespace Thetacat.Metatags.Model;

public class BuiltinTags
{
    // these are built-in mediatags. 
    public static readonly Guid s_CatRootID = new Guid("F154F44E-8075-4BAC-A9C2-167D5815C195");
    public static readonly Guid s_WidthID = new Guid("B84B3981-C69C-4C5A-9928-377B8FEC5FBC");
    public static readonly Guid s_HeightID = new Guid("FE34A67F-DD8C-41F8-A914-EE1E5628D292");
    public static readonly Guid s_OriginalMediaDateID = new Guid("BDE93371-2422-489E-86AD-462E2EC975CD");
    public static readonly Guid s_ImportDateID = new Guid("DA8D46B7-10AE-4B69-9EDD-AB4E97F6AB4B");

    public static readonly Metatag s_Width = Metatag.Create(s_CatRootID, "width", "width", MetatagStandards.Standard.Cat, s_WidthID);
    public static readonly Metatag s_Height = Metatag.Create(s_CatRootID, "height", "height", MetatagStandards.Standard.Cat, s_HeightID);
    public static readonly Metatag s_OriginalMediaDate = Metatag.Create(s_CatRootID, "originalMediaDate", "originalMediaDate", MetatagStandards.Standard.Cat, s_OriginalMediaDateID);
    public static readonly Metatag s_ImportDate = Metatag.Create(s_CatRootID, "importDate", "importDate", MetatagStandards.Standard.Cat, s_ImportDateID);
}
