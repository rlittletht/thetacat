using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

public class MetatagBuilder
{
    private Metatag m_building = new();

    public static MetatagBuilder Create()
    {
        MetatagBuilder builder = new();

        return builder;
    }

    public MetatagBuilder SetName(string name)
    {
        m_building.Name = name;
        return this;
    }

    public MetatagBuilder SetID(string id)
    {
        m_building.ID = id;
        return this;
    }

    public MetatagBuilder SetParentID(string parentID)
    {
        m_building.ParentID = parentID;
        return this;
    }

    public MetatagBuilder SetParentName(string parentName)
    {
        m_building.ParentName = parentName;
        return this;
    }

    public MetatagBuilder SetElementsTypeName(string typeName)
    {
        m_building.ElementsTypeName = typeName;
        return this;
    }

    public Metatag Build()
    {
        return m_building;
    }
}