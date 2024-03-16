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
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        TestMetatagTree.AssertList("metatag1:metatag3_1:metatag5_3_1:metatag2:metatag6_2:metatag7:", schemaDef.Metatags);
        TestMetatagTree.AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag3_1(2)[#]:metatag5_3_1(3)[#]:metatag2(1)[#]:metatag6_2(2)[#]:metatag7(1)[#]:", schemaDef.Tree);
    }

    [Test]
    public static void TestDeleteLastChildOfRoot()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        schemaDef.FRemoveMetatag(TestMetatags.metatagId7);

        TestMetatagTree.AssertList("metatag1:metatag3_1:metatag5_3_1:metatag2:metatag6_2:", schemaDef.Metatags);
        TestMetatagTree.AssertTree(
            "___Root(0)[#]:metatag1(1)[#]:metatag3_1(2)[#]:metatag5_3_1(3)[#]:metatag2(1)[#]:metatag6_2(2)[#]:",
            schemaDef.Tree);
    }

    [Test]
    public static void TestDeleteLeafDeep()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagSchemaDefinition schemaDef = new MetatagSchemaDefinition();

        foreach (Metatag metatag in metatags)
        {
            schemaDef.AddMetatag(metatag);
        }

        schemaDef.FRemoveMetatag(TestMetatags.metatagId5);

        TestMetatagTree.AssertList("metatag1:metatag3_1:metatag2:metatag6_2:metatag7:", schemaDef.Metatags);
        TestMetatagTree.AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag3_1(2)[#]:metatag2(1)[#]:metatag6_2(2)[#]:metatag7(1)[#]:", schemaDef.Tree);
    }
}
