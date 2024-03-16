using Thetacat.Metatags.Model;
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

    public static readonly ServiceMetatag s_metatag1 = new ServiceMetatag() { ID = metatagId1, Name = "metatag1", Description = "test:metatag1", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2 = new ServiceMetatag() { ID = metatagId2, Name =   "metatag2",   Description = "test:metatag2",   Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2N = new ServiceMetatag() { ID = metatagId2, Name =  "metatag2N",  Description = "test:metatag2",   Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2D = new ServiceMetatag() { ID = metatagId2, Name =  "metatag2",   Description = "test:metatag2D",  Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2S = new ServiceMetatag() { ID = metatagId2, Name =  "metatag2",   Description = "test:metatag2",   Parent = null, Standard = "userS" };
    public static readonly ServiceMetatag s_metatag2N2 = new ServiceMetatag() { ID = metatagId2, Name = "metatag2N2", Description = "test:metatag2",   Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2D2 = new ServiceMetatag() { ID = metatagId2, Name = "metatag2",   Description = "test:metatag2D2", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag2S2 = new ServiceMetatag() { ID = metatagId2, Name = "metatag2",   Description = "test:metatag2",   Parent = null, Standard = "userS2" };
    public static readonly ServiceMetatag s_metatag3 = new ServiceMetatag() { ID = metatagId3, Name = "metatag3", Description = "test:metatag3", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag4 = new ServiceMetatag() { ID = metatagId4, Name = "metatag4", Description = "test:metatag4", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag5 = new ServiceMetatag() { ID = metatagId5, Name = "metatag5", Description = "test:metatag5", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag6 = new ServiceMetatag() { ID = metatagId6, Name = "metatag6", Description = "test:metatag6", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag7 = new ServiceMetatag() { ID = metatagId7, Name = "metatag7", Description = "test:metatag7", Parent = null, Standard = "user" };
    public static readonly ServiceMetatag s_metatag8 = new ServiceMetatag() { ID = metatagId8, Name = "metatag8", Description = "test:metatag8", Parent = null, Standard = "user" };

    public static readonly Metatag metatag1 = Metatag.CreateFromService(s_metatag1);
    public static readonly Metatag metatag2 = Metatag.CreateFromService(s_metatag2);
    public static readonly Metatag metatag2N = Metatag.CreateFromService(s_metatag2N);
    public static readonly Metatag metatag2D = Metatag.CreateFromService(s_metatag2D);
    public static readonly Metatag metatag2S = Metatag.CreateFromService(s_metatag2S);
    public static readonly Metatag metatag2N2 = Metatag.CreateFromService(s_metatag2N2);
    public static readonly Metatag metatag2D2 = Metatag.CreateFromService(s_metatag2D2);
    public static readonly Metatag metatag2S2 = Metatag.CreateFromService(s_metatag2S2);
    public static readonly Metatag metatag3 = Metatag.CreateFromService(s_metatag3);
    public static readonly Metatag metatag4 = Metatag.CreateFromService(s_metatag4);
    public static readonly Metatag metatag5 = Metatag.CreateFromService(s_metatag5);
    public static readonly Metatag metatag6 = Metatag.CreateFromService(s_metatag6);
    public static readonly Metatag metatag7 = Metatag.CreateFromService(s_metatag7);
    public static readonly Metatag metatag8 = Metatag.CreateFromService(s_metatag8);


    public static readonly ServiceMetatag s_metatag3_1 = new ServiceMetatag() { ID = metatagId3, Name = "metatag3_1", Description = "metatag3:metatag1", Parent = metatagId1, Standard = "user" };
    public static readonly ServiceMetatag s_metatag3_1P = new ServiceMetatag() { ID = metatagId3, Name = "metatag3_1", Description = "metatag3:metatag1", Parent = metatagId2, Standard = "user" };
    public static readonly ServiceMetatag s_metatag3_1P2 = new ServiceMetatag() { ID = metatagId3, Name = "metatag3_1", Description = "metatag3:metatag1", Parent = metatagId3, Standard = "user" };
    public static readonly ServiceMetatag s_metatag4_1 = new ServiceMetatag() { ID = metatagId4, Name = "metatag4_1", Description = "metatag4:metatag1", Parent = metatagId1, Standard = "user" };
    public static readonly ServiceMetatag s_metatag5_3_1 = new ServiceMetatag() { ID = metatagId5, Name = "metatag5_3_1", Description = "test:metatag5_3_1", Parent = metatagId3, Standard = "user" };
    public static readonly ServiceMetatag s_metatag6_2 = new ServiceMetatag() { ID = metatagId6, Name = "metatag6_2", Description = "metatag2:metatag6", Parent = metatagId2, Standard = "user" };

    public static readonly Metatag metatag3_1 = Metatag.CreateFromService(s_metatag3_1);
    public static readonly Metatag metatag3_1P = Metatag.CreateFromService(s_metatag3_1P);
    public static readonly Metatag metatag3_1P2 = Metatag.CreateFromService(s_metatag3_1P2);
    public static readonly Metatag metatag4_1 = Metatag.CreateFromService(s_metatag4_1);
    public static readonly Metatag metatag5_3_1 = Metatag.CreateFromService(s_metatag5_3_1);
    public static readonly Metatag metatag6_2 = Metatag.CreateFromService(s_metatag6_2);

}
