using System.Collections.ObjectModel;
using Thetacat.Util;

namespace Tests.Util;

public class TestDistributedObservableCollection
{
    public class TestItem
    {
        public int Data { get; set; }

        public TestItem(int data)
        {
            Data = data;
        }

        public bool IsEqual(TestItem right)
        {
            if (Data != right.Data) return false;

            return true;
        }
    }

    public class TestItemLine : IObservableSegmentableCollectionHolder<TestItem>
    {
        private readonly ObservableCollection<TestItem> m_items = new ObservableCollection<TestItem>();

        public ObservableCollection<TestItem> Items => m_items;
        public bool EndSegmentAfter { get; set; } = false;
        public int LineData { get; set; }

        public TestItemLine()
        {}

        public TestItemLine(IEnumerable<TestItem> items, bool endAfter, int lineData)
        {
            foreach (TestItem item in items)
            {
                Items.Add(item);
            }
            EndSegmentAfter = endAfter;
            LineData = lineData;
        }

        public bool IsEqual(TestItemLine right)
        {
            if (EndSegmentAfter != right.EndSegmentAfter) return false;
            if (LineData != right.LineData) return false;
            if (Items.Count != right.Items.Count) return false;
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].IsEqual(right.Items[i])) return false;
            }

            return true;
        }
    }

    public static TestItemLine LineFactory(TestItemLine? refLine)
    {
        return new TestItemLine();
    }

    public static void MoveLineProps(TestItemLine from, TestItemLine to)
    {
        bool val = from.EndSegmentAfter;
        from.EndSegmentAfter = false;
        to.EndSegmentAfter = val;
    }

    static void AreEqual(TestItemLine[] expected, DistributedObservableCollection<TestItemLine, TestItem> actual)
    {
        int segmentsActual = 0;
        int expectedLine = 0;

        foreach (TestItemLine item in actual.TopCollection)
        {
            Assert.IsTrue(expected[expectedLine].IsEqual(item));
            if (item.EndSegmentAfter)
                segmentsActual++;
            expectedLine++;
        }

        Assert.AreEqual(segmentsActual, actual.SegmentCount);
        Assert.AreEqual(expectedLine, expected.Length);
        Assert.AreEqual(expected.Length, actual.TopCollection.Count);
    }

    [Test]
    public static void TestOneSegment_ShrinkingLine_NoShift()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, true, 0)
            };

        AreEqual(expectedLines, collection);
    }

    [Test]
    public static void TestOneSegment_ShrinkingLine_MultipleLines_NoNewLine()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(3), new TestItem(4), }, true, 0)
            };

        AreEqual(expectedLines, collection);
    }

    [Test]
    public static void TestOneSegment_ShrinkingLine_MultipleLines_MultipleNewLines()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
                new TestItem(5),
                new TestItem(6),
            });

        collection.UpdateItemsPerLine(2);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(2), new TestItem(3), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(4), new TestItem(5), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(6), }, true, 0),
            };

        AreEqual(expectedLines, collection);
    }

    [Test]
    public static void TestOneSegment_ShrinkingLine_MultipleLines_MultipleNewLines_OnePerLine()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
                new TestItem(5),
                new TestItem(6),
            });

        collection.UpdateItemsPerLine(1);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(1), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(2), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(3), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(4), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(5), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(6), }, true, 0),
            };

        AreEqual(expectedLines, collection);
    }

    [Test]
    public static void TestOneSegment_ShrinkingLine_MultipleLines_OneNewLine()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
                new TestItem(5),
                new TestItem(6),
                new TestItem(7),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(3), new TestItem(4), new TestItem(5), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(6), new TestItem(7), }, true, 0)
            };

        AreEqual(expectedLines, collection);
    }


    [Test]
    public static void TestOneSegment_ShrinkingLine_MultipleLines_OneNewLineOneItem()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
                new TestItem(5),
                new TestItem(6),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(3), new TestItem(4), new TestItem(5), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(6), }, true, 0)
            };

        AreEqual(expectedLines, collection);
    }


    [Test]
    public static void TestTwoSegment_ShrinkingLine_FirstNoChange_Second_MultipleLines_OneNewLineOneItem()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
            });

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(10),
                new TestItem(11),
                new TestItem(12),
                new TestItem(13),
                new TestItem(14),
                new TestItem(15),
                new TestItem(16),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, true, 0),
                new TestItemLine(new TestItem[] { new TestItem(10), new TestItem(11), new TestItem(12), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(13), new TestItem(14), new TestItem(15), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(16), }, true, 0)
            };

        AreEqual(expectedLines, collection);
    }

    [Test]
    public static void TestTwoSegment_ShrinkingLine_FirstMultipleLines_OneNewLineOneItem_SecondNoChange()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(10),
                new TestItem(11),
                new TestItem(12),
                new TestItem(13),
                new TestItem(14),
                new TestItem(15),
                new TestItem(16),
            });

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(10), new TestItem(11), new TestItem(12), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(13), new TestItem(14), new TestItem(15), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(16), }, true, 0),
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, true, 0),
            };

        AreEqual(expectedLines, collection);
    }


    [Test]
    public static void TestTwoSegment_ShrinkingLine_BothMultipleLines_OneNewLineOneItem()
    {
        DistributedObservableCollection<TestItemLine, TestItem> collection = new(LineFactory, MoveLineProps);

        collection.UpdateItemsPerLine(4);

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(0),
                new TestItem(1),
                new TestItem(2),
                new TestItem(3),
                new TestItem(4),
                new TestItem(5),
                new TestItem(6),
                new TestItem(7),
            });

        collection.AddSegment(
            new TestItem[]
            {
                new TestItem(10),
                new TestItem(11),
                new TestItem(12),
                new TestItem(13),
                new TestItem(14),
                new TestItem(15),
                new TestItem(16),
                new TestItem(17),
            });

        collection.UpdateItemsPerLine(3);

        TestItemLine[] expectedLines =
            new TestItemLine[]
            {
                new TestItemLine(new TestItem[] { new TestItem(0), new TestItem(1), new TestItem(2), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(3), new TestItem(4), new TestItem(5), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(6), new TestItem(7), }, true, 0),
                new TestItemLine(new TestItem[] { new TestItem(10), new TestItem(11), new TestItem(12), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(13), new TestItem(14), new TestItem(15), }, false, 0),
                new TestItemLine(new TestItem[] { new TestItem(16), new TestItem(17), }, true, 0),
            };

        AreEqual(expectedLines, collection);
    }
}
