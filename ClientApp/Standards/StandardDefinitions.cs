using System.Collections.Generic;

namespace Thetacat.Standards;

public class StandardDefinitions
{
    public string Tag { get; init; }
    public MetatagStandards.Standard StandardId { get; init; }
    public Dictionary<int, StandardDefinition> Properties { get; init; }
    public string[] TypeNames { get; init; }

    public StandardDefinitions(MetatagStandards.Standard standardId, string tag, string[] typeNames, Dictionary<int, StandardDefinition> properties)
    {
        StandardId = standardId;
        Tag = tag;
        TypeNames = typeNames;
        Properties = properties;
    }
}
