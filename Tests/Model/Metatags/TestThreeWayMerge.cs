using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;

namespace Tests.Model.Metatags;

public class TestThreeWayMerge
{
    static MetatagSchemaDefinition BuildTestDefinition(IEnumerable<ServiceMetatag> tags, int version)
    {
        ServiceMetatagSchema serviceSchema =
            new ServiceMetatagSchema()
            {
                SchemaVersion = version,
                Metatags = new(tags)
            };

        return new MetatagSchemaDefinition(serviceSchema);
    }

    static (MetatagSchemaDefinition, MetatagSchemaDefinition, MetatagSchemaDefinition)
        BuildTestFiles(
            IEnumerable<ServiceMetatag> bases, int baseVersion,
            IEnumerable<ServiceMetatag> servers, int serverVersion,
            IEnumerable<ServiceMetatag> locals, int localVersion
            )
    {
        return (
            BuildTestDefinition(bases, baseVersion),
            BuildTestDefinition(servers, serverVersion),
            BuildTestDefinition(locals, localVersion));
    }

    private static MetatagSchemaDiff BuildDiff(IEnumerable<MetatagSchemaDiffOp>? ops, int baseVersion, int targetVersion)
    {
        MetatagSchemaDiff diff = new MetatagSchemaDiff(baseVersion, targetVersion);

        if (ops != null)
        {
            foreach (MetatagSchemaDiffOp op in ops)
            {
                diff.AddDiffOp(op);
            }
        }

        return diff;
    }

    private static void AssertAreEqualDiffs(
        MetatagSchemaDiff expected,
        MetatagSchemaDiff actual)
    {
        Assert.AreEqual(expected.TargetSchemaVersion, actual.TargetSchemaVersion);
        Assert.AreEqual(expected.BaseSchemaVersion, actual.BaseSchemaVersion);

        Assert.AreEqual(expected.GetDiffCount, actual.GetDiffCount);

        Dictionary<Guid, MetatagSchemaDiffOp> actualMap = new();

        foreach (MetatagSchemaDiffOp op in actual.Ops)
        {
            actualMap.Add(op.ID, op);
        }

        foreach (MetatagSchemaDiffOp op in expected.Ops)
        {
            if (!actualMap.TryGetValue(op.ID, out MetatagSchemaDiffOp? opActual))
                Assert.Fail();
            else
            {
                Assert.AreEqual(op.Action, opActual.Action);
                Assert.AreEqual(op.ID, opActual.ID);
                if (op.Action != MetatagSchemaDiffOp.ActionType.Delete)
                {
                    Assert.AreEqual(op.IsDescriptionChanged, opActual.IsDescriptionChanged);
                    Assert.AreEqual(op.IsParentChanged, opActual.IsParentChanged);
                    Assert.AreEqual(op.IsNameChanged, opActual.IsNameChanged);
                    Assert.AreEqual(op.IsStandardChanged, opActual.IsStandardChanged);
                    Assert.AreEqual(op.Metatag, opActual.Metatag);
                }
            }
        }
    }

    [Test]
    public static void TestIdentityMerge()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) = 
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1,  // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1,  // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1); // local

        MetatagSchemaDiff diffExpected = BuildDiff(null, 1, 2);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, local, server);
        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void TestIdentityMerge_DifferentSchemaVersions()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1,  // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2,  // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2); // local

        MetatagSchemaDiff diffExpected = BuildDiff(null, 2, 3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, local, server);
        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalAddsTag()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1,  // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2,  // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2, TestMetatags.s_metatag3 }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new []
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Insert, TestMetatags.metatag3, false, false, false, false) }, 
                2,
                3);
                
        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_ServerAddsTag()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2, TestMetatags.s_metatag3 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2); // local

        // server added a tag, but we did nothing
        MetatagSchemaDiff diffExpected = BuildDiff(null, 2, 3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalAndLocalAddsTag()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag4 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Insert, TestMetatags.metatag2, false, false, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalDeletesTag()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2, // server
                new[] { TestMetatags.s_metatag1 }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Delete, TestMetatags.metatag2, false, false, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_ServerDeletesTag()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2); // local

        MetatagSchemaDiff diffExpected = BuildDiff(null, 2, 3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_Local_EditName()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2N }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2N, true, false, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_Local_EditDescription()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2D }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2D, false, true, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_Local_EditStandard()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2S }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2S, false, false, true, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_Local_EditParent()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1P }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag3_1P, false, false, false, true) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    // now for some colliding edits
    [Test]
    public static void Test_LocalServer_EditName()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2N2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2N }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2N, true, false, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalServer_EditDescription()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2D2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2D }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2D, false, true, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalServer_EditStandard()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2S2 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag2S }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag2S, false, false, true, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalServer_EditParent()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1P }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1P2 }, 2); // local

        // in this case, the server's parentID should win, so there is no diff at all
        MetatagSchemaDiff diffExpected =
            BuildDiff(null, 2, 3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }

    [Test]
    public static void Test_LocalServer_EditNameDescriptionParent()
    {
        (MetatagSchemaDefinition _base, MetatagSchemaDefinition server, MetatagSchemaDefinition local) =
            BuildTestFiles(
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3 }, 1, // base
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1 }, 2, // server
                new[] { TestMetatags.s_metatag1, TestMetatags.s_metatag3_1P }, 2); // local

        MetatagSchemaDiff diffExpected =
            BuildDiff(
                new[]
                { MetatagSchemaDiffOp.CreateForTest(MetatagSchemaDiffOp.ActionType.Update, TestMetatags.metatag3_1, true, true, false, false) },
                2,
                3);

        MetatagSchemaDiff diff = MetatagSchema.DoThreeWayMergeFromDefinitions(_base, server, local);

        AssertAreEqualDiffs(diffExpected, diff);
    }
}
