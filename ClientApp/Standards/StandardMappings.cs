using System.Collections.Generic;

namespace Thetacat.Standards;

public class StandardMappings
{
    public string Tag { get; init; }
    public MetatagStandards.Standard StandardId { get; init; }
    public Dictionary<int, StandardMapping> Properties { get; init; }
    public string[] TypeNames { get; init; }

    public StandardMappings(MetatagStandards.Standard standardId, string tag, string[] typeNames, Dictionary<int, StandardMapping> properties)
    {
        StandardId = standardId;
        Tag = tag;
        TypeNames = typeNames;
        Properties = properties;
    }
}
