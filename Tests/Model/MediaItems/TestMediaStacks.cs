using Thetacat.Model;

namespace Tests.Model.MediaItems;

public class TestMediaStacks
{
    [Test]
    public static void TestMediaStacksCreateEnumerator_OneItemAtEnd()
    {
        MediaStacks stacks = new MediaStacks("version");

        stacks.AddStack(new MediaStack("version", ""));
        stacks.CreateNewStack("new1");

        MediaStack[] expected =
            {
                new MediaStack("version", "new1")
            };

        int i = 0;
        foreach (MediaStack stack in stacks.GetPendingCreates())
        {
            Assert.AreEqual(expected[i].Type, stack.Type);
            Assert.AreEqual(expected[i].Description, stack.Description);
            i++;
        }

        Assert.AreEqual(expected.Length, i);
    }

    [Test]
    public static void TestMediaStacksCreateEnumerator_OneItemAtStart()
    {
        MediaStacks stacks = new MediaStacks("version");

        stacks.CreateNewStack("new1");
        stacks.AddStack(new MediaStack("version", ""));

        MediaStack[] expected =
        {
            new MediaStack("version", "new1")
        };

        int i = 0;
        foreach (MediaStack stack in stacks.GetPendingCreates())
        {
            Assert.AreEqual(expected[i].Type, stack.Type);
            Assert.AreEqual(expected[i].Description, stack.Description);
            i++;
        }

        Assert.AreEqual(expected.Length, i);
    }

    [Test]
    public static void TestMediaStacksCreateEnumerator_OneItemOnlyMatch()
    {
        MediaStacks stacks = new MediaStacks("version");

        stacks.CreateNewStack("new1");

        MediaStack[] expected =
        {
            new MediaStack("version", "new1")
        };

        int i = 0;
        foreach (MediaStack stack in stacks.GetPendingCreates())
        {
            Assert.AreEqual(expected[i].Type, stack.Type);
            Assert.AreEqual(expected[i].Description, stack.Description);
            i++;
        }

        Assert.AreEqual(expected.Length, i);
    }

    [Test]
    public static void TestMediaStacksCreateEnumerator_OneItemNoMatch()
    {
        MediaStacks stacks = new MediaStacks("version");

        stacks.AddStack(new MediaStack("version", ""));

        MediaStack[] expected =
        {
        };

        int i = 0;
        foreach (MediaStack stack in stacks.GetPendingCreates())
        {
            Assert.AreEqual(expected[i].Type, stack.Type);
            Assert.AreEqual(expected[i].Description, stack.Description);
            i++;
        }

        Assert.AreEqual(expected.Length, i);
    }

}
