using System.Collections.Generic;

namespace Thetacat.Standards;

public class StandardMappings
{
    public string Tag { get; init; }
    public StandardMapping[] Properties { get; init; }
    public string[] TypeNames { get; init; }

    public StandardMappings(string tag, string[] typeNames, StandardMapping[] properties)
    {
        Tag = tag;
        TypeNames = typeNames;
        Properties = properties;
    }
}
