using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

public class ElementsMetatagBuilder
{
    private ElementsMetatag m_building = new();

    public static ElementsMetatagBuilder Create()
    {
        ElementsMetatagBuilder builder = new();

        return builder;
    }

    public ElementsMetatagBuilder SetName(string name)
    {
        m_building.Name = name;
        return this;
    }

    public ElementsMetatagBuilder SetID(string id)
    {
        m_building.ID = id;
        return this;
    }

    public ElementsMetatagBuilder SetParentID(string parentID)
    {
        m_building.ParentID = parentID;
        return this;
    }

    public ElementsMetatagBuilder SetParentName(string parentName)
    {
        m_building.ParentName = parentName;
        return this;
    }

    public ElementsMetatagBuilder SetElementsTypeName(string typeName)
    {
        m_building.ElementsTypeName = typeName;
        return this;
    }

    public ElementsMetatag Build()
    {
        return m_building;
    }
}