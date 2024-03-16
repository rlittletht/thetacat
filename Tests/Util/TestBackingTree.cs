using System.Text;
using Thetacat.Types;

namespace Tests.Util;

public class TestBackingTree
{
    class StringTestData : IBackingTreeItemData
    {
        public string Name { get; }
        public string Description { get; }
        public bool? Checked { get; set; }

        public StringTestData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    static KeyValuePair<string?, StringTestData>[] StringSplitter(StringTestData data)
    {
        // simple string splitter
        string[] split = data.Name.Split("/");
        List<KeyValuePair<string?, StringTestData>> newData = new();

        int i = 0;
        for(; i < split.Length - 1; i++)
        {
            string s = split[i];
            newData.Add(new KeyValuePair<string?, StringTestData>(s, new StringTestData(s, s)));
        }

        if (i == split.Length - 1)
            newData.Add(new KeyValuePair<string?, StringTestData>(null, new StringTestData(split[i], split[i])));

        return newData.ToArray();
    }

    [TestCase(new[] { "foo/bar/foobar.txt", "foo/bar.txt" }, "__ROOT(0)[#]:foo(1)[#]:bar.txt(2)[#]:bar(2)[#]:foobar.txt(3)[#]:")]
    [TestCase(new[] { "foobar.txt", "bar.txt" }, "__ROOT(0)[#]:foobar.txt(1)[#]:bar.txt(1)[#]:")]
    [TestCase(new[] { "foo/bar", "foo/bar/foobar.txt", "foo/bar.txt" }, "__ROOT(0)[#]:foo(1)[#]:bar(2)[#]:bar.txt(2)[#]:bar(2)[#]:foobar.txt(3)[#]:")]
    [Test]
    public static void CreateTreeTest(string[] list, string expected)
    {
        List<StringTestData> dataList = new();
        foreach (string s in list)
        {
            dataList.Add(new StringTestData(s, s));
        }

        BackingTree tree = BackingTree.CreateFromList<StringTestData, string>(dataList, StringSplitter, new StringTestData("!!ROOT", "!!ROOT"));

        StringBuilder actual = new StringBuilder();

        BackingTreeItem<StringTestData>.Preorder(
            tree,
            null,
            (item, _, depth) =>
            {
                string isChecked = item.Checked == null ? "#" : (item.Checked == true ? "X" : " ");
                actual.Append($"{item.Name}({depth})[{isChecked}]:");
            },
            0);
        Assert.AreEqual(expected, actual.ToString());
    }
}
