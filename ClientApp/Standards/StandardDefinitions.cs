using System.Collections.Generic;

namespace Thetacat.Standards;

public class StandardDefinitions
{
    public string StandardTag { get; init; }
    public MetatagStandards.Standard StandardId { get; init; }
    public Dictionary<int, StandardDefinition> Properties { get; init; }
    public string[] TypeNames { get; init; }

    public StandardDefinitions(MetatagStandards.Standard standardId, string standardTag, string[] typeNames, Dictionary<int, StandardDefinition> properties)
    {
        StandardId = standardId;
        StandardTag = standardTag;
        TypeNames = typeNames;
        Properties = properties;
    }
}
