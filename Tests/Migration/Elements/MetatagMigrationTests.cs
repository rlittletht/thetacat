using Emgu.CV.Dnn;
using Thetacat;
using Thetacat.Migration.Elements.Metadata.UI;
using NUnit.Framework;
using Thetacat.Model;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;

public class MetatagMigrationTests
{
    private static readonly Metatag userRoot = Metatag.Create(null, "user", "user root", MetatagStandards.Standard.User);

    [Test]
    public void TestBuildTagsToInsert_OneRoot_NoChildren_NoExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot
            };

        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        Assert.AreEqual("Root", tagsToInsert[0].Metatag.Name);
        Assert.AreEqual(userRoot.ID, tagsToInsert[0].Metatag.Parent);
        Assert.AreEqual(1, tagsToInsert.Count);
    }

    [Test]
    public void TestBuildTagsToInsert_TwoRoots_NoChildren_NoExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot
            };
        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected =
            new()
            {
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build(), "1"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root2").SetDescription("Root2").SetParentID(userRoot.ID).Build(), "2")
            };
        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }

    [Test]
    public void TestBuildTagsToInsert_TwoRoots_NoChildren_OneExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot,
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root2").SetDescription("Root2").SetParentID(userRoot.ID).Build()
            };

        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected =
            new()
            {
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build(), "1")
            };
        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }

    [Test]
    public void TestBuildTagsToInsert_TwoRoots_NoChildren_BothExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot,
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root2").SetDescription("Root2").SetParentID(userRoot.ID).Build(),
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build()
            };

        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected = new();
        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }

    [Test]
    public void TestBuildTagsToInsert_TwoRoots_BothWithOne_NoneExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot
            };


        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);
        
        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 11,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root-Child1",
                                           ParentID = 1,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 21,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2-Child1",
                                           ParentID = 2,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected =
            new()
            {
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build(), "1"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root-Child1").SetDescription("Root:Root-Child1").SetParentID(tagsToInsert[0].Metatag.ID).Build(), "11"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root2").SetDescription("Root2").SetParentID(userRoot.ID).Build(), "2"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root2-Child1").SetDescription("Root2:Root2-Child1").SetParentID(tagsToInsert[2].Metatag.ID).Build(), "21")
            };

        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }
    [Test]

    public void TestBuildTagsToInsert_TwoRoots_BothWithOne_ParentsExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot,
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root2").SetDescription("Root2").SetParentID(userRoot.ID).Build(),
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build()
            };

        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 11,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root-Child1",
                                           ParentID = 1,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 21,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2-Child1",
                                           ParentID = 2,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected =
            new()
            {
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root-Child1").SetDescription("Root:Root-Child1").SetParentID(liveTags[2].ID).Build(), "11"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root2-Child1").SetDescription("Root2:Root2-Child1").SetParentID(liveTags[1].ID).Build(), "21")
            };

        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }

    public void TestBuildTagsToInsert_TwoRoots_BothWithOne_OneParentExisting()
    {
        List<Thetacat.Model.Metatag> liveTags =
            new()
            {
                userRoot,
                Thetacat.Metatags.MetatagBuilder.Create(Guid.NewGuid()).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build()
            };

        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<PseMetatag> tagsToSync = new()
                                   {
                                       new PseMetatag()
                                       {
                                           ID = 1,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 2,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2",
                                           ParentID = 0,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 11,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root-Child1",
                                           ParentID = 1,
                                           ParentName = ""
                                       },
                                       new PseMetatag()
                                       {
                                           ID = 21,
                                           ElementsTypeName = string.Empty,
                                           Name = "Root2-Child1",
                                           ParentID = 2,
                                           ParentName = ""
                                       }
                                   };

        List<MetatagPair> tagsToInsert = UserMetatagMigration.BuildTagsToInsert(liveTree, new PseMetatagTree(tagsToSync), userRoot);

        List<MetatagPair> tagsExpected =
            new()
            {
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root").SetDescription("Root").SetParentID(userRoot.ID).Build(), "1"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root-Child1").SetDescription("Root:Root-Child1").SetParentID(tagsToInsert[0].Metatag.ID).Build(), "11"),
                new MetatagPair(Thetacat.Metatags.MetatagBuilder.Create(Thetacat.Model.Metatag.IdMatchAny).SetName("Root2-Child1").SetDescription("Root2:Root2-Child1").SetParentID(liveTags[1].ID).Build(), "21")
            };

        Assert.AreEqual(tagsExpected.Count, tagsToInsert.Count);
        Assert.AreEqual(tagsExpected, tagsToInsert);
    }
}
