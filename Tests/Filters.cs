using TCore.PostfixText;
using Thetacat.Filtering;

namespace Tests;

public class FilterTests
{
    [TestCase(new[] { "one" }, true, "[one] == '$true' ")]
    [TestCase(new[] { "one", "two" }, true, "[one] == '$true' [two] == '$true' || ")]
    [TestCase(new[] { "one", "two" }, false, "[one] == '$true' [two] == '$true' && ")]
    [TestCase(new[] { "one", "two", "three" }, true, "[one] == '$true' [two] == '$true' || [three] == '$true' || ")]
    [TestCase(new[] { "one", "two", "three", "four" }, true, "[one] == '$true' [two] == '$true' || [three] == '$true' [four] == '$true' || || ")]
    [TestCase(new[] { "0", "1", "2", "3" }, true, "[0] == '$true' [1] == '$true' || [2] == '$true' [3] == '$true' || || ")]
    [TestCase(new[] { "0", "1", "2", "3", "4" }, true, "[0] == '$true' [1] == '$true' || [2] == '$true' || [3] == '$true' [4] == '$true' || || ")]
    [TestCase(new[] { "0", "1", "2", "3", "4", "5" }, true, "[0] == '$true' [1] == '$true' || [2] == '$true' || [3] == '$true' [4] == '$true' || [5] == '$true' || || ")]
    [TestCase(new[] { "0", "1", "2", "3", "4", "5", "6" }, true, "[0] == '$true' [1] == '$true' || [2] == '$true' [3] == '$true' || || [4] == '$true' [5] == '$true' || [6] == '$true' || || ")]
    [TestCase(new[] { "0", "1", "2", "3", "4", "5", "6", "7" }, true, "[0] == '$true' [1] == '$true' || [2] == '$true' [3] == '$true' || || [4] == '$true' [5] == '$true' || [6] == '$true' [7] == '$true' || || || ")]
    [Test]
    public static void TestBuildClause(string[] strings, bool fAny, string expected)
    {
        PostfixText postfix = new PostfixText();
        PostfixOperator op = new PostfixOperator(fAny ? PostfixOperator.Op.Or : PostfixOperator.Op.And);
        List<string> stringList = new List<string>(strings);

        Filters.BuildClause(stringList, 0, strings.Length - 1, postfix, op);

        Assert.AreEqual(expected, postfix.ToString());
    }
}
