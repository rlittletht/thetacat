using System.Diagnostics.CodeAnalysis;

namespace Thetacat.Standards;

public class StandardDefinition
{
    public int Tag { get; init; }
    public string TagName { get; init; }
    public bool Include { get; init; }
    public StandardDefinition(int tag, string tagName, bool include = true)
    {
        Tag = tag;
        TagName = tagName;
        Include = include;
    }
}
