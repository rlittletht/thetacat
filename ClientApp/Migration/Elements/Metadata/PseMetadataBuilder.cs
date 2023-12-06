using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements.Metadata;

public class PseMetadataBuilder
{
    private readonly PseMetadata m_building = new();

    public static PseMetadataBuilder Create()
    {
        PseMetadataBuilder builder = new();

        return builder;
    }

    public PseMetadataBuilder SetPseId(int pseId)
    {
        m_building.PseID = pseId;
        return this;
    }

    public PseMetadataBuilder SetID(Guid id)
    {
        m_building.ID = id;
        return this;
    }

    public PseMetadataBuilder SetPseIdentifier(string pseIdentifier)
    {
        m_building.PseIdentifier = pseIdentifier;
        return this;
    }

    public PseMetadataBuilder Standard(string standard)
    {
        m_building.Standard = standard;
        return this;
    }

    public PseMetadataBuilder SetTag(string tag)
    {
        m_building.Tag = tag;
        return this;
    }

    public PseMetadataBuilder SetMigrate(bool migrate)
    {
        m_building.Migrate= migrate;
        return this;
    }

    public PseMetadataBuilder SetPseDatatype(string datatype)
    {
        m_building.PseDatatype = datatype;
        return this;
    }
    
    public PseMetadata Build()
    {
        return m_building;
    }
}