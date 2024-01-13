using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;

namespace Tests.Model;

public class TestMetatags
{
    public static readonly Guid metatagId1 = Guid.Parse("00000000-1000-0000-0000-000000000001");
    public static readonly Guid metatagId2 = Guid.Parse("00000000-2000-0000-0000-000000000002");
    public static readonly Guid metatagId3 = Guid.Parse("00000000-3000-0000-0000-000000000003");
    public static readonly Guid metatagId4 = Guid.Parse("00000000-4000-0000-0000-000000000004");
    public static readonly Guid metatagId5 = Guid.Parse("00000000-5000-0000-0000-000000000005");
    public static readonly Guid metatagId6 = Guid.Parse("00000000-6000-0000-0000-000000000006");
    public static readonly Guid metatagId7 = Guid.Parse("00000000-7000-0000-0000-000000000007");
    public static readonly Guid metatagId8 = Guid.Parse("00000000-8000-0000-0000-000000000008");

    public static readonly Metatag metatag1 = new Metatag() { ID = metatagId1, Name = "metatag1", Description = "test:metatag1", Parent = null, Standard = "user" };
    public static readonly Metatag metatag2 = new Metatag() { ID = metatagId2, Name = "metatag2", Description = "test:metatag2", Parent = null, Standard = "user" };
    public static readonly Metatag metatag3 = new Metatag() { ID = metatagId3, Name = "metatag3", Description = "test:metatag3", Parent = null, Standard = "user" };
    public static readonly Metatag metatag4 = new Metatag() { ID = metatagId4, Name = "metatag4", Description = "test:metatag4", Parent = null, Standard = "user" };
    public static readonly Metatag metatag5 = new Metatag() { ID = metatagId5, Name = "metatag5", Description = "test:metatag5", Parent = null, Standard = "user" };
    public static readonly Metatag metatag6 = new Metatag() { ID = metatagId6, Name = "metatag6", Description = "test:metatag6", Parent = null, Standard = "user" };
    public static readonly Metatag metatag7 = new Metatag() { ID = metatagId7, Name = "metatag7", Description = "test:metatag7", Parent = null, Standard = "user" };
    public static readonly Metatag metatag8 = new Metatag() { ID = metatagId8, Name = "metatag8", Description = "test:metatag8", Parent = null, Standard = "user" };

    public static readonly Metatag metatag1_3 = new Metatag() { ID = metatagId3, Name = "metatag1_3", Description = "metatag1:metatag3", Parent = metatagId1, Standard = "user" };
    public static readonly Metatag metatag1_4 = new Metatag() { ID = metatagId4, Name = "metatag1_4", Description = "metatag1:metatag4", Parent = metatagId1, Standard = "user" };
    public static readonly Metatag metatag1_3_5 = new Metatag() { ID = metatagId5, Name = "metatag1_3_5", Description = "test:metatag1_3_5", Parent = metatagId3, Standard = "user" };
    public static readonly Metatag metatag2_6 = new Metatag() { ID = metatagId6, Name = "metatag2_6", Description = "metatag2:metatag6", Parent = metatagId2, Standard = "user" };
}
