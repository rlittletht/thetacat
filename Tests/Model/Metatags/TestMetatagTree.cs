using System.Collections.ObjectModel;
using System.Text;
using Thetacat.Metatags;
using Thetacat.Model.Metatags;

namespace Tests.Model.Metatags;

public class TestMetatagTree
{
    public static void AssertTree(string expectedPreorder, MetatagTree tree)
    {
        StringBuilder actual = new StringBuilder();

        tree.Preorder(
            (item, depth) =>
            {
                string isChecked = item.Checked == null ? "#" : (item.Checked == true ? "X" : " ");
                actual.Append($"{item.Name}({depth})[{isChecked}]:");
            },
            0);

        Assert.AreEqual(expectedPreorder, actual.ToString());
    }

    [Test]
    public static void TestNoNesting_NoneChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3 }
            );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag2(1)[ ]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_NoneChecked_EmptyCheckedList()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag2(1)[ ]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_NoneChecked_MiddleChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new()
            {
                { TestMetatags.metatagId2.ToString(), true }
            };

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag2(1)[X]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_NoneChecked_UnknownItemChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new()
            {
                { TestMetatags.metatagId4.ToString(), true }
            };

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag2(1)[ ]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestSimpleNesting_NoneChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_4
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag1_3(2)[ ]:metatag1_4(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestSimpleNesting_LastAndFirstItemsChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_4
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new()
            {
                { TestMetatags.metatagId1.ToString(), null },
                { TestMetatags.metatagId4.ToString(), true },
            };

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag1_3(2)[ ]:metatag1_4(2)[X]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToAll()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag2(1)[ ]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToNone()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        List<Metatag> metatagsInclude = new();

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToUnknown()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag4, TestMetatags.metatag5, TestMetatags.metatag6
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToOne()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag2, TestMetatags.metatag3
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag3
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag3(1)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToOneLeaf()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_4,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag1_4
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag1_4(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToMiddle()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_3_5,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag1_3
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag1_3(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToDeepestLeaf()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag1_3, TestMetatags.metatag1_3_5,
                TestMetatags.metatag2, TestMetatags.metatag2_6,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag1_3_5
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag1_3(2)[ ]:metatag1_3_5(3)[ ]:", treeClone);
    }

}
