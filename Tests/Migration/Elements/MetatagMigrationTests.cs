
using Thetacat;
using Thetacat.Migration.Elements;
using NUnit.Framework;

public class MetatagMigrationTests
{
    [Test]
    public void TestBuildTagsToInsert_OneRoot_NoChildren_NoExisting()
    {
        List<Thetacat.Model.Metatag> liveTags = new();
        Thetacat.Metatags.MetatagTree liveTree = new(liveTags);

        List<Metatag> tagsToSync = new()
                                   {
                                       new Metatag()
                                       {
                                           ID = "1",
                                           ElementsTypeName = string.Empty,
                                           Name = "Root",
                                           ParentID = "0",
                                           ParentName = ""
                                       }
                                   };

        List<Thetacat.Model.Metatag> tagsToInsert = MetatagMigration.BuildTagsToInsert(liveTree, tagsToSync);

        Assert.AreEqual("Root", tagsToInsert[0].Name);
        Assert.AreEqual(null, tagsToInsert[0].Parent);
        Assert.AreEqual(1, tagsToInsert.Count);
    }
}