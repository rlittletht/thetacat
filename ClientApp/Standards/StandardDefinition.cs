using System.Diagnostics.CodeAnalysis;

namespace Thetacat.Standards;

public class StandardDefinition
{
    public int PropertyTag { get; init; }
    public string PropertyTagName { get; init; }
    public bool Include { get; init; }
    public StandardDefinition(int propertyTag, string propertyTagName, bool include = true)
    {
        PropertyTag = propertyTag;
        PropertyTagName = propertyTagName;
        Include = include;
    }
}
