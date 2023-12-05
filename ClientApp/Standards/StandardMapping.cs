using System.Diagnostics.CodeAnalysis;

namespace Thetacat.Standards;

public class StandardMapping
{
    public int Tag { get; init; }
    public string TagName { get; init; }
    public bool Include { get; init; }
    public StandardMapping(int tag, string tagName, bool include = true)
    {
        Tag = tag;
        TagName = tagName;
        Include = include;
    }
}
