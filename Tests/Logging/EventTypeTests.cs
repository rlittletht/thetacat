using Thetacat.Logging;

namespace Tests.Logging;

public class EventTypeTests
{
    [TestCase(EventType.Critical, "Critical")]
    [TestCase(EventType.Error, "Error")]
    [TestCase(EventType.Information, "Information")]
    [TestCase(EventType.Verbose, "Verbose")]
    [TestCase(EventType.Warning, "Warning")]
    [Test]
    public static void TestEventTypeToString(EventType eventType, string expectedString)
    {
        Assert.AreEqual(expectedString, eventType.ToString());
    }

    [TestCase("Critical", EventType.Critical)]
    [TestCase("Error", EventType.Error)]
    [TestCase("Information", EventType.Information)]
    [TestCase("Verbose", EventType.Verbose)]
    [TestCase("Warning", EventType.Warning)]
    [Test]
    public static void TestEventTypeFromString(string eventString, EventType eventExpected)
    {
        Assert.AreEqual(eventExpected, Enum.Parse<EventType>(eventString));
    }
}
