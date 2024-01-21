using Thetacat.Metatags.Model;

namespace Tests.Model.Metatags;

public class TestMetatagSchemaDefinition
{
    [Test]
    public static void TestBaseCase()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_3_5,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        TestMetatagTree.AssertList("metatag1:metatag1_3:metatag1_3_5:metatag2:metatag2_6:metatag7:", schemaDef.Metatags);
        TestMetatagTree.AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag1_3(2)[#]:metatag1_3_5(3)[#]:metatag2(1)[#]:metatag2_6(2)[#]:metatag7(1)[#]:", schemaDef.Tree);
    }

    [Test]
    public static void TestDeleteLastChildOfRoot()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_3_5,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        schemaDef.FRemoveMetatag(TestMetatags.metatagId7);

        TestMetatagTree.AssertList("metatag1:metatag1_3:metatag1_3_5:metatag2:metatag2_6:", schemaDef.Metatags);
        TestMetatagTree.AssertTree(
            "___Root(0)[#]:metatag1(1)[#]:metatag1_3(2)[#]:metatag1_3_5(3)[#]:metatag2(1)[#]:metatag2_6(2)[#]:",
            schemaDef.Tree);
    }

    [Test]
    public static void TestDeleteLeafDeep()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_3_5,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        schemaDef.FRemoveMetatag(TestMetatags.metatagId5);

        TestMetatagTree.AssertList("metatag1:metatag1_3:metatag2:metatag2_6:metatag7:", schemaDef.Metatags);
        TestMetatagTree.AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag1_3(2)[#]:metatag2(1)[#]:metatag2_6(2)[#]:metatag7(1)[#]:", schemaDef.Tree);
    }
}
