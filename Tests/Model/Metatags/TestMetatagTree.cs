﻿using System.Text;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;

namespace Tests.Model.Metatags;

public class TestMetatagTree
{
    public static void AssertTree(string expectedPreorder, MetatagTree tree)
    {
        StringBuilder actual = new StringBuilder();

        tree.Preorder(
            null,
            (item, _, depth) =>
            {
                string isChecked = item.Checked == null ? "#" : (item.Checked == true ? "X" : " ");
                actual.Append($"{item.Name}({depth})[{isChecked}]:");
            },
            0);

        Assert.AreEqual(expectedPreorder, actual.ToString());
    }

    public static void AssertList(string expectedList, IEnumerable<Metatag> list)
    {
        StringBuilder actual = new StringBuilder();

        foreach (Metatag item in list)
        {
            actual.Append($"{item.Name}:");
        }

        Assert.AreEqual(expectedList, actual.ToString());
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
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag4_1
            }
        );

        MetatagTree tree = new(metatags);
        MetatagTree treeClone = new();
        Dictionary<string, bool?> initialCheckedState =
            new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, initialCheckedState);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag3_1(2)[ ]:metatag4_1(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestSimpleNesting_LastAndFirstItemsChecked()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag4_1
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

        AssertTree("___Root(0)[#]:metatag1(1)[#]:metatag3_1(2)[ ]:metatag4_1(2)[X]:", treeClone);
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
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag4_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag4_1
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag4_1(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToMiddle()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag3_1
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag3_1(2)[ ]:", treeClone);
    }

    [Test]
    public static void TestNoNesting_FilterToDeepestLeaf()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        List<Metatag> metatagsInclude = new(
            new[]
            {
                TestMetatags.metatag5_3_1
            }
        );

        MetatagTree tree = new(metatags, null, metatagsInclude);
        MetatagTree treeClone = new();

        MetatagTree.CloneAndSetCheckedItems(tree.Children, treeClone.Children, null);

        AssertTree("___Root(0)[#]:metatag1(1)[ ]:metatag3_1(2)[ ]:metatag5_3_1(3)[ ]:", treeClone);
    }

    [Test]
    public static void TestFindParent_1Deep_WithChildren()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagTree tree = new(metatags);

        IMetatagTreeItem? parent = tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(TestMetatags.metatagId3));

        Assert.AreEqual("metatag1", parent!.Name);
    }

    [Test]
    public static void TestFindParent_1Deep_SecondChild()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagTree tree = new(metatags);

        IMetatagTreeItem? parent = tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(TestMetatags.metatagId6));

        Assert.AreEqual("metatag2", parent!.Name);
    }

    [Test]
    public static void TestFindParent_2Deep_Leaf()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagTree tree = new(metatags);

        IMetatagTreeItem? parent = tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(TestMetatags.metatagId5));

        Assert.AreEqual("metatag3_1", parent!.Name);
    }

    [Test]
    public static void TestFindParent_RootIsParent()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagTree tree = new(metatags);

        IMetatagTreeItem? parent = tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(TestMetatags.metatagId1));

        Assert.AreEqual("___Root", parent!.Name);
    }

    [Test]
    public static void TestFindParent_RootIsParent_LastChild()
    {
        List<Metatag> metatags = new(
            new[]
            {
                TestMetatags.metatag1, TestMetatags.metatag3_1, TestMetatags.metatag5_3_1,
                TestMetatags.metatag2, TestMetatags.metatag6_2,
                TestMetatags.metatag7
            }
        );

        MetatagTree tree = new(metatags);

        IMetatagTreeItem? parent = tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(TestMetatags.metatagId7));

        Assert.AreEqual("___Root", parent!.Name);
    }

}
