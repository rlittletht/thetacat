using System.Collections.Generic;

namespace Thetacat.Standards;

public class StandardMappings
{
    public string Tag { get; init; }
    public StandardsMappings.Builtin BuiltinId { get; init; }
    public Dictionary<int, StandardMapping> Properties { get; init; }
    public string[] TypeNames { get; init; }

    public StandardMappings(StandardsMappings.Builtin builtinId, string tag, string[] typeNames, Dictionary<int, StandardMapping> properties)
    {
        BuiltinId = builtinId;
        Tag = tag;
        TypeNames = typeNames;
        Properties = properties;
    }
}
