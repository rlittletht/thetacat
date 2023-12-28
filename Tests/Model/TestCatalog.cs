using Thetacat.Model;

namespace Tests.Model;

public class TestCatalog
{
    static void AreEqual(Dictionary<Guid, MediaStackItem> expected, MediaStack stack)
    {
        foreach (MediaStackItem item in stack.Items)
        {
            Assert.AreEqual(expected[item.MediaId], item);
            expected.Remove(item.MediaId);
        }

        Assert.AreEqual(0, expected.Count);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToEmptyStack()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 0);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 0) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }


    [Test]
    public static void TestAddMediaToStackAtIndex_AddToSingleItemStack_NoConflict()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 0));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 1);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 0) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 1) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_NoConflict()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 0));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 1));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 2);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 0) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 1) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 2) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_NoConflict_RequiresRenumber()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 0));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 0));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 1);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 0) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 2) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 1) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_Conflict()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 0));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 1));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 1);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 0) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 2) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 1) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_Conflict_RenumberRequired()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 1));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 1));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 1);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 2) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 3) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 1) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_MultipleConflicts_RenumberRequired()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 1));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 1));
        stack.Items.Add(new MediaStackItem(TestMedia.media4, 0));
        stack.Items.Add(new MediaStackItem(TestMedia.media5, 0));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 1);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 2) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 3) },
                { TestMedia.media4, new MediaStackItem(TestMedia.media4, 0) },
                { TestMedia.media5, new MediaStackItem(TestMedia.media5, 4) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 1) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }

    [Test]
    public static void TestAddMediaToStackAtIndex_AddToTwoItemsStack_NoConflict_MultipleRenumberRequired()
    {
        Catalog catalog = new Catalog();

        MediaStacks stacks = catalog.GetStacksFromType(MediaStackType.Version);
        MediaStack stack = stacks.CreateNewStack();
        stack.Items.Add(new MediaStackItem(TestMedia.media2, 1));
        stack.Items.Add(new MediaStackItem(TestMedia.media3, 1));
        stack.Items.Add(new MediaStackItem(TestMedia.media4, 0));
        stack.Items.Add(new MediaStackItem(TestMedia.media5, 0));

        catalog.AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, TestMedia.media1, 2);

        Dictionary<Guid, MediaStackItem> expected =
            new()
            {
                { TestMedia.media2, new MediaStackItem(TestMedia.media2, 1) },
                { TestMedia.media3, new MediaStackItem(TestMedia.media3, 3) },
                { TestMedia.media4, new MediaStackItem(TestMedia.media4, 0) },
                { TestMedia.media5, new MediaStackItem(TestMedia.media5, 4) },
                { TestMedia.media1, new MediaStackItem(TestMedia.media1, 2) }
            };

        AreEqual(expected, stacks.Items[stack.StackId]);
    }
}
