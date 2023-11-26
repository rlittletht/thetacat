using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Thetacat.Controls;
using Thetacat.Types;

namespace Thetacat.Migration.Elements;

/// <summary>
/// Interaction logic for MediaMigration.xaml
/// </summary>
public partial class MetatagMigration
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestBuildTagsToInsert_OneRoot_NoChildren_NoExisting()
        {
            List<Model.Metatag> liveTags = new();
            Metatags.MetatagTree liveTree = new Metatags.MetatagTree(liveTags);

            List<Metatag> tagsToSync = new List<Metatag>()
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

            List<Model.Metatag> tagsToInsert = BuildTagsToInsert(liveTree, tagsToSync);

            Assert.AreEqual("Root", tagsToInsert[0].Name);
            Assert.AreEqual(null, tagsToInsert[0].Parent);
            Assert.AreEqual(1, tagsToInsert.Count);
        }
    }
}

[TestFixture]
public class TestIt
{
    [Test]
    public void StaticTest()
    {
        Assert.AreEqual(1, 1);
    }
}