using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements.Metadata;

public class PseMetatagBuilder
{
    private readonly PseMetatag m_building = new();

    public static PseMetatagBuilder Create()
    {
        PseMetatagBuilder builder = new();

        return builder;
    }

    public PseMetatagBuilder SetName(string name)
    {
        m_building.Name = name;
        return this;
    }

    public PseMetatagBuilder SetID(string id)
    {
        m_building.ID = id;
        return this;
    }

    public PseMetatagBuilder SetParentID(string parentID)
    {
        m_building.ParentID = parentID;
        return this;
    }

    public PseMetatagBuilder SetParentName(string parentName)
    {
        m_building.ParentName = parentName;
        return this;
    }

    public PseMetatagBuilder SetElementsTypeName(string typeName)
    {
        m_building.ElementsTypeName = typeName;
        return this;
    }

    public PseMetatag Build()
    {
        return m_building;
    }
}